using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class JobManager : Singleton<JobManager>
{
    private NativeList<JobHandle> jobs;

    private void Awake()
    {
        jobs = new NativeList<JobHandle>(Allocator.Persistent);
        jobs.Clear();
    }
    private void Update()
    {
        if (!jobs.IsEmpty)
        {
            JobHandle.CompleteAll(jobs);
        }

        jobs.Clear();
    }

    public void ScheduleJobForCompletion(JobHandle j)
    {
        jobs.Add(j);
    }
}
