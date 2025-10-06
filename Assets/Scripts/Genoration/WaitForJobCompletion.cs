using UnityEngine;
using Unity.Jobs;

public class WaitForJobCompletion : CustomYieldInstruction
{
    private JobHandle m_Handle;

    public WaitForJobCompletion(JobHandle handle)
    {
        m_Handle = handle;
    }

    public override bool keepWaiting
    {
        get
        {
            return !m_Handle.IsCompleted;
        }
    }
}