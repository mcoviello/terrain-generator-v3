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
    }
    private void Update()
    {
        JobHandle.CompleteAll(jobs);
    }

    public void ScheduleJobForCompletion(JobHandle j)
    {
        jobs.Add(j);
    }

    private void OnDestroy()
    {
        jobs.Dispose();
    }
}
