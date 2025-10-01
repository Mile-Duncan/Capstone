using System.Collections.Generic;
// Removed unused 'UnityEditor.PackageManager'
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics; // Used for float3 and distances

public static class RailPlacer
{
    private static float snapDistance = 1.5f;
    private static float tangentLength = 3.0f; // INCREASED: Controls the curve of the newly created segment, now smoother

    // --- State Management for Two-Step Placement ---
    private enum PlacementState { WaitingForFirstKnot, WaitingForSecondKnot }
    private static PlacementState currentState = PlacementState.WaitingForFirstKnot;

    private struct KnotData
    {
        public Vector3 worldPosition;
        public bool didSnap;
        public Spline splineToBranchFrom; // The spline the start knot snapped to (if any)
        public int knotIndex;            // The index of the knot it snapped to
        public bool isEndKnot;           // NEW: True if the knot is at index 0 or Count - 1
    }
    private static KnotData firstKnot;
    // ----------------------------------------------

    /// <summary>
    /// Handles the input for placing a new rail segment. This method should be
    /// called twice per segment: once for the start point and once for the end point.
    /// </summary>
    /// <param name="worldPosition">The desired world position for the new knot.</param>
    public static void HandlePlacementInput(Vector3 worldPosition) // Made method static
    {
        // NOTE: You will need to ensure RailNetwork.Instance.railSplines is initialized and not null 
        // before calling this static method in your project.
        if (RailNetwork.Instance == null || RailNetwork.Instance.railSplines == null)
        {
            Debug.LogError("RailNetwork Instance or SplineContainer is null. Cannot place rail.");
            return;
        }

        if (currentState == PlacementState.WaitingForFirstKnot)
        {
            ProcessFirstKnot(worldPosition);
        }
        else // currentState == PlacementState.WaitingForSecondKnot
        {
            ProcessSecondKnot(worldPosition);
        }
    }

    /// <summary>
    /// Step 1: Determines the start point of the new segment and checks for snapping.
    /// </summary>
    private static void ProcessFirstKnot(Vector3 worldPosition)
    {
        // UPDATED: FindNearestKnot now returns isEndKnot
        (Vector3 snappedPos, Spline snappedSpline, int knotIndex, bool isEndKnot) = FindNearestKnot(worldPosition);

        if (snappedSpline != null)
        {
            // SNAP OCCURRED: Initialize a branch/switch point
            firstKnot.didSnap = true;
            firstKnot.worldPosition = snappedPos;
            firstKnot.splineToBranchFrom = snappedSpline;
            firstKnot.knotIndex = knotIndex;
            firstKnot.isEndKnot = isEndKnot; // NEW: Store whether it's an end knot
            
            string snapType = isEndKnot ? "END knot" : "MIDDLE knot";
            Debug.Log($"First knot snapped to existing {snapType} at index {knotIndex}. Waiting for second input.");
        }
        else
        {
            // NO SNAP: Start a new, isolated segment
            firstKnot.didSnap = false;
            firstKnot.worldPosition = worldPosition;
            firstKnot.splineToBranchFrom = null;
            firstKnot.knotIndex = -1;
            firstKnot.isEndKnot = false;
            Debug.Log("First knot placed, no snap. Waiting for second input to create a new track.");
        }

        currentState = PlacementState.WaitingForSecondKnot;
    }

    /// <summary>
    /// Step 2: Determines the end point of the new segment and finalizes placement.
    /// </summary>
    private static void ProcessSecondKnot(Vector3 worldPosition)
    {
        // 1. Check for snapping on the second input
        (Vector3 snappedPos, Spline snappedSpline, int knotIndex, bool isEndKnot) secondSnap = FindNearestKnot(worldPosition);
        
        Vector3 knotAPosition;
        Vector3 knotBPosition;
        string debugMessage = "";
        Spline newSpline = null; // Initialize to null
        bool secondDidSnap = secondSnap.snappedSpline != null; 

        // --- NEW LOGIC ORDER: Prioritize EXTENSION, LOOP, and MERGE over SPLIT/NEW ---

        if (firstKnot.didSnap && firstKnot.isEndKnot)
        {
            // Case 1: EXTEND, MERGE, or LOOP (First knot snapped to an END knot)
            Spline targetSpline = firstKnot.splineToBranchFrom;
            
            if (secondDidSnap && secondSnap.isEndKnot)
            {
                // Check 1.A: CLOSING A LOOP on the same spline
                if (targetSpline == secondSnap.snappedSpline)
                {
                    // Check if the snap is to the opposite end (index 0 vs Count-1)
                    bool isOppositeEnd = 
                        (firstKnot.knotIndex == 0 && secondSnap.knotIndex == targetSpline.Count - 1) ||
                        (firstKnot.knotIndex == targetSpline.Count - 1 && secondSnap.knotIndex == 0);

                    if (isOppositeEnd)
                    {
                        // Set the spline to loop, connecting the two end knots
                        // REFINEMENT: Assigning to a local variable might help the compiler resolve the type.
                        Spline splineToLoop = targetSpline;
                        splineToLoop.Closed= true;
                        UpdateLoopTangents(splineToLoop); 

                        currentState = PlacementState.WaitingForFirstKnot;
                        Debug.Log($"Track loop successfully CLOSED.");
                        return; // EXIT early
                    }
                }
                // Check 1.B: MERGE/BRIDGE two different splines
                else if (targetSpline != secondSnap.snappedSpline) 
                {
                    // If the second snap is to a *different* spline's end, we will fall through 
                    // to Case 2: BRIDGE TRACK (New Spline Creation) to create the segment between them.
                    newSpline = null; // Forces execution of subsequent logic
                }
            }

            // If we are here and newSpline is null, it means we are EXTENDING the targetSpline
            if (newSpline == null)
            {
                // Convert world position to local position relative to the SplineContainer
                Vector3 localPositionB = RailNetwork.Instance.railSplines.transform.InverseTransformPoint(worldPosition);
                BezierKnot newKnot = new BezierKnot((float3)localPositionB, float3.zero, float3.zero);

                // Add the new knot to the target spline (extension)
                int newKnotIndex;
                if (firstKnot.knotIndex == 0) // Snapped to the start knot
                {
                    targetSpline.Insert(0, newKnot); // Extend backward
                    newKnotIndex = 0;
                }
                else // Snapped to the end knot (Count - 1)
                {
                    targetSpline.Add(newKnot); // Extend forward
                    newKnotIndex = targetSpline.Count - 1;
                }
                
                // Update tangents for the extended spline (the new knot and the pivot knot)
                UpdateSplineExtensionTangents(targetSpline, newKnotIndex);

                currentState = PlacementState.WaitingForFirstKnot;
                Debug.Log($"Track EXTENDED successfully. New knot at index {newKnotIndex}.");
                return; // EXIT early, no new spline created.
            }
        }
        
        // --- Remaining Cases (New Spline Creation) ---

        if (firstKnot.didSnap && secondDidSnap)
        {
            // Case 2: BRIDGE TRACK (Both knots snapped, but not an extension or loop)
            knotAPosition = firstKnot.worldPosition;
            knotBPosition = secondSnap.snappedPos; 
            
            newSpline = new Spline();
            RailNetwork.Instance.railSplines.AddSpline(newSpline);

            AddNewKnot(newSpline, knotAPosition);
            AddNewKnot(newSpline, knotBPosition);
            
            debugMessage = $"Bridge created! Connecting existing knot {firstKnot.knotIndex} to knot {secondSnap.knotIndex}. Note: This creates a separate spline segment.";

            // Update tangents on existing knots for smooth branching/connecting
            UpdateExistingKnotTangentForBranch(firstKnot.splineToBranchFrom, firstKnot.knotIndex, knotBPosition, isIncomingTangent: false);
            UpdateExistingKnotTangentForBranch(secondSnap.snappedSpline, secondSnap.knotIndex, knotAPosition, isIncomingTangent: true);
        }
        else if (firstKnot.didSnap) // Implies firstKnot.isEndKnot is false OR first knot didn't snap to an end knot.
        {
            // Case 3: SPLIT/BRANCH TRACK (Only first knot snapped, NOT to an end)
            knotAPosition = firstKnot.worldPosition;
            knotBPosition = worldPosition; // Use raw position for the end of the new branch
            
            newSpline = new Spline();
            RailNetwork.Instance.railSplines.AddSpline(newSpline);

            AddNewKnot(newSpline, knotAPosition);
            AddNewKnot(newSpline, knotBPosition);
            
            debugMessage = $"Switch created! New spline branches from knot {firstKnot.knotIndex} on its parent spline and ends at {knotBPosition}";
            
            // Update tangent on existing knot for smooth branching
            UpdateExistingKnotTangentForBranch(firstKnot.splineToBranchFrom, firstKnot.knotIndex, knotBPosition, isIncomingTangent: false);
        }
        else if (secondDidSnap) // Equivalent to (!firstKnot.didSnap && secondDidSnap)
        {
            // Case 4: NEW SEGMENT SNAPPING TO EXISTING TRACK (Only second knot snapped)
            knotAPosition = firstKnot.worldPosition;
            knotBPosition = secondSnap.snappedPos; // Use the snapped position to ensure clean connection

            newSpline = new Spline();
            RailNetwork.Instance.railSplines.AddSpline(newSpline);

            AddNewKnot(newSpline, knotAPosition);
            AddNewKnot(newSpline, knotBPosition);
            
            debugMessage = $"New track segment created, starting isolated at {knotAPosition} and snapping to existing knot at index {secondSnap.knotIndex}.";
            
            // Update tangent on existing knot for smooth connection
            UpdateExistingKnotTangentForBranch(secondSnap.snappedSpline, secondSnap.knotIndex, knotAPosition, isIncomingTangent: true);
        }
        else
        {
            // Case 5: NEW ISOLATED TRACK (Neither knot snapped)
            knotAPosition = firstKnot.worldPosition;
            knotBPosition = worldPosition; // Use raw position

            newSpline = new Spline();
            RailNetwork.Instance.railSplines.AddSpline(newSpline);

            AddNewKnot(newSpline, knotAPosition);
            AddNewKnot(newSpline, knotBPosition);
            
            debugMessage = $"New isolated track created between {knotAPosition} and {knotBPosition}";
        }
        
        // --- Apply Curvature to newly created two-knot segments (Cases 2, 3, 4, 5) ---
        if (newSpline != null)
        {
            ApplyCurvatureToNewSegment(newSpline, knotAPosition, knotBPosition);
        }

        // Reset state for the next segment placement
        currentState = PlacementState.WaitingForFirstKnot;
        Debug.Log(debugMessage);
    }

    /// <summary>
    /// Updates the tangents for the first and last knots when a spline is set to loop (closed).
    /// </summary>
    private static void UpdateLoopTangents(Spline spline)
    {
        if (spline.Count < 2) return;

        // Last Knot (index Count - 1)
        int lastIndex = spline.Count - 1;
        BezierKnot lastKnot = spline[lastIndex];
        
        // First Knot (index 0)
        BezierKnot firstKnot = spline[0];

        // 1. Get local positions
        Vector3 localPosLast = (Vector3)lastKnot.Position;
        Vector3 localPosFirst = (Vector3)firstKnot.Position;
        
        // 2. Vector from Last to First
        Vector3 segmentVector = localPosFirst - localPosLast;

        // 3. Define the tangents
        // Last Knot: TangentOut points towards the next knot (which is the first knot)
        float3 outTangentLast = (float3)(segmentVector.normalized * tangentLength);
        
        // First Knot: TangentIn points away from the previous knot (which is the last knot)
        float3 inTangentFirst = (float3)(-segmentVector.normalized * tangentLength);

        // 4. Apply and re-assign
        lastKnot.TangentOut = outTangentLast;
        spline[lastIndex] = lastKnot;

        firstKnot.TangentIn = inTangentFirst;
        spline[0] = firstKnot;
    }


    /// <summary>
    /// Updates the tangents for the two most recent knots in an extended spline to ensure smoothness.
    /// This method is called when a spline is extended (knot added at 0 or Count-1).
    /// </summary>
    /// <param name="extendedSpline">The spline that was just modified.</param>
    /// <param name="newKnotIndex">The index of the newly added knot (0 or Count - 1).</param>
    private static void UpdateSplineExtensionTangents(Spline extendedSpline, int newKnotIndex)
    {
        // Get the indices of the two knots that define the new segment
        int pivotKnotIndex;
        int newKnotFinalIndex;

        if (newKnotIndex == 0) // Extended backward (new knot is at 0, pivot is at 1)
        {
            newKnotFinalIndex = 0;
            pivotKnotIndex = 1;
        }
        else // Extended forward (new knot is at Count-1, pivot is at Count-2)
        {
            newKnotFinalIndex = extendedSpline.Count - 1;
            pivotKnotIndex = extendedSpline.Count - 2;
        }

        BezierKnot pivotKnot = extendedSpline[pivotKnotIndex];
        BezierKnot newKnot = extendedSpline[newKnotFinalIndex];

        // 1. Get local positions for vector calculation
        Vector3 localPosPivot = (Vector3)pivotKnot.Position;
        Vector3 localPosNew = (Vector3)newKnot.Position;

        // 2. Calculate the segment vector (Vector from Pivot to New)
        Vector3 segmentVector = localPosNew - localPosPivot;

        // 3. Define the tangents 
        float3 outTangentPivot = (float3)(segmentVector.normalized * tangentLength); 
        float3 inTangentNew = (float3)(-segmentVector.normalized * tangentLength); 

        // 4. Apply tangents
        
        // Pivot Knot: Update TangentOut to point towards the new knot
        pivotKnot.TangentOut = outTangentPivot;
        extendedSpline[pivotKnotIndex] = pivotKnot;

        // New Knot: Update TangentIn to point away from the pivot knot
        newKnot.TangentIn = inTangentNew;
        extendedSpline[newKnotFinalIndex] = newKnot;
    }


    /// <summary>
    /// Updates the tangent of an existing knot on an existing spline to point towards a target position, 
    /// ensuring a smooth transition where a new spline connects or branches.
    /// </summary>
    /// <param name="existingSpline">The spline being modified.</param>
    /// <param name="knotIndex">The index of the knot on the existing spline.</param>
    /// <param name="directionTargetWorldPosition">The world position of the other end of the new segment.</param>
    /// <param name="isIncomingTangent">If true, calculates TangentIn (for ending connections). If false, calculates TangentOut (for starting branches).</param>
    private static void UpdateExistingKnotTangentForBranch(Spline existingSpline, int knotIndex, Vector3 directionTargetWorldPosition, bool isIncomingTangent = false)
    {
        // 1. Get the local position of the existing knot
        BezierKnot knot = existingSpline[knotIndex]; 
        
        // 2. We need the local position of the target point relative to the *container*.
        Transform containerTransform = RailNetwork.Instance.railSplines.transform;
        Vector3 targetLocalPos = containerTransform.InverseTransformPoint(directionTargetWorldPosition);

        // 3. Calculate the vector from the existing knot to the target point
        Vector3 branchVector = targetLocalPos - (Vector3)knot.Position; // Vector is (Knot -> Target)

        // 4. Calculate the new tangent vector
        float3 newTangent;
        
        if (isIncomingTangent)
        {
            // For an incoming connection (ending snap), TangentIn should point AWAY from the direction of connection (towards the knot itself).
            newTangent = (float3)(-branchVector.normalized * tangentLength);
        }
        else
        {
            // For an outgoing branch (starting snap), TangentOut should point TOWARDS the new segment.
            newTangent = (float3)(branchVector.normalized * tangentLength);
        }

        // 5. Update the existing knot (must re-assign the struct)
        BezierKnot existingKnot = existingSpline[knotIndex];
        
        if (isIncomingTangent)
        {
            // Keep existing TangentOut, update TangentIn
            existingKnot.TangentIn = newTangent;
        }
        else
        {
            // Keep existing TangentIn, update TangentOut
            existingKnot.TangentOut = newTangent;
        }
        
        existingSpline[knotIndex] = existingKnot;
    }


    /// <summary>
    /// Calculates the vector between the two new knots and applies appropriate tangents
    /// to create a smooth, curved segment. This is only used for brand new two-knot splines.
    /// </summary>
    private static void ApplyCurvatureToNewSegment(Spline newSpline, Vector3 worldPosA, Vector3 worldPosB)
    {
        // 1. Get local positions for vector calculation
        Vector3 localPosA = RailNetwork.Instance.railSplines.transform.InverseTransformPoint(worldPosA);
        Vector3 localPosB = RailNetwork.Instance.railSplines.transform.InverseTransformPoint(worldPosB);
        
        // 2. Calculate the segment vector (Vector from A to B)
        Vector3 segmentVector = localPosB - localPosA;

        // 3. Define the tangents
        // Outgoing tangent for Knot A (points towards B)
        float3 outTangentA = (float3)(segmentVector.normalized * tangentLength); 
        
        // Incoming tangent for Knot B (points away from A, towards the knot)
        float3 inTangentB = (float3)(-segmentVector.normalized * tangentLength); 

        // 4. Update Knot A (index 0)
        BezierKnot knotA = newSpline[0];
        knotA.TangentOut = outTangentA;
        newSpline[0] = knotA; // Must re-assign the struct back

        // 5. Update Knot B (index 1)
        BezierKnot knotB = newSpline[1];
        knotB.TangentIn = inTangentB;
        newSpline[1] = knotB; // Must re-assign the struct back
    }

    /// <summary>
    /// Searches all existing knots in the container to find the closest one within snapDistance.
    /// </summary>
    /// <param name="worldPosition">The point to check against.</param>
    /// <returns>A tuple containing the snapped position, the spline it belongs to (or null), the knot index, and if it's an end knot.</returns>
    private static (Vector3, Spline, int, bool) FindNearestKnot(Vector3 worldPosition)
    {
        float minSqDistance = snapDistance * snapDistance;
        Vector3 nearestKnotWorldPosition = Vector3.zero;
        Spline nearestSpline = null;
        int nearestKnotIndex = -1;
        bool didSnap = false;

        foreach (Spline currentSpline in RailNetwork.Instance.railSplines.Splines)
        {
            for (int i = 0; i < currentSpline.Count; i++)
            {
                float3 knotLocalPosition = currentSpline[i].Position; 
                
                // Convert local position to world position using the SplineContainer's transform.
                Vector3 worldKnotV3 = RailNetwork.Instance.railSplines.transform.TransformPoint((Vector3)knotLocalPosition);
                float3 knotWorldPosition = (float3)worldKnotV3;
                
                float sqDistance = math.distancesq(knotWorldPosition, (float3)worldPosition);

                if (sqDistance < minSqDistance)
                {
                    minSqDistance = sqDistance;
                    nearestKnotWorldPosition = (Vector3)knotWorldPosition;
                    nearestSpline = currentSpline;
                    nearestKnotIndex = i;
                    didSnap = true;
                }
            }
        }

        if (didSnap)
        {
            // Determine if the found knot is an end knot (index 0 or last index)
            bool isEnd = (nearestKnotIndex == 0 || nearestKnotIndex == nearestSpline.Count - 1);
            return (nearestKnotWorldPosition, nearestSpline, nearestKnotIndex, isEnd);
        }

        return (worldPosition, null, -1, false);
    }

    /// <summary>
    /// Helper method to create and add a new BezierKnot to the spline.
    /// </summary>
    private static void AddNewKnot(Spline spline, Vector3 position)
    {
        // Convert world position to local position relative to the SplineContainer
        Vector3 localPosition = RailNetwork.Instance.railSplines.transform.InverseTransformPoint(position);

        // Create a new Bezier knot with zero tangents 
        BezierKnot newKnot = new BezierKnot(
            (float3)localPosition,
            float3.zero, // Incoming tangent (TangentIn)
            float3.zero  // Outgoing tangent (TangentOut)
        );

        // Add the knot to the end of the spline
        spline.Add(newKnot);
    }

    /*
     * EXAMPLE USAGE IN EDITOR/RUNTIME CONTROLLER:
     * This method shows how you might call HandlePlacementInput from a placement
     * controller (requires a collider, e.g., a terrain or plane).
     *
     * public void HandlePlacementClick()
     * {
     * Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
     * if (Physics.Raycast(ray, out RaycastHit hit))
     * {
     * RailPlacer.HandlePlacementInput(hit.point); // Now static
     * }
     * }
     */
}
