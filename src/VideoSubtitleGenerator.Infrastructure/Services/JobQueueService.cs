using System.Collections.ObjectModel;
using VideoSubtitleGenerator.Core;
using VideoSubtitleGenerator.Core.Enums;
using VideoSubtitleGenerator.Core.Interfaces;
using VideoSubtitleGenerator.Core.Models;

namespace VideoSubtitleGenerator.Infrastructure.Services;

/// <summary>
/// In-memory implementation of job queue service
/// </summary>
public class JobQueueService : IJobQueueService
{
    private readonly ObservableCollection<TranscriptionJob> _jobs;
    private readonly object _lock = new();

    public event EventHandler<JobEventArgs>? JobStatusChanged;

    public JobQueueService()
    {
        _jobs = new ObservableCollection<TranscriptionJob>();
    }

    public void EnqueueJobs(IEnumerable<TranscriptionJob> jobs)
    {
        try
        {
            lock (_lock)
            {
                foreach (var job in jobs)
                {
                    _jobs.Add(job);
                    OnJobStatusChanged(job);
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            throw;
        }
    }

    public TranscriptionJob? DequeueJob()
    {
        try
        {
            lock (_lock)
            {
                return _jobs.FirstOrDefault(j => j.Status == JobStatus.Pending);
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            return null;
        }
    }

    public IReadOnlyList<TranscriptionJob> GetAllJobs()
    {
        try
        {
            lock (_lock)
            {
                return _jobs.ToList().AsReadOnly();
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            return new List<TranscriptionJob>().AsReadOnly();
        }
    }

    public TranscriptionJob? GetJobById(Guid id)
    {
        try
        {
            lock (_lock)
            {
                return _jobs.FirstOrDefault(j => j.Id == id);
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            return null;
        }
    }

    public void UpdateJob(TranscriptionJob job)
    {
        try
        {
            lock (_lock)
            {
                var existingJob = _jobs.FirstOrDefault(j => j.Id == job.Id);
                if (existingJob != null)
                {
                    var index = _jobs.IndexOf(existingJob);
                    _jobs[index] = job;
                    OnJobStatusChanged(job);
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            throw;
        }
    }

    public void ClearCompleted()
    {
        try
        {
            lock (_lock)
            {
                var completedJobs = _jobs
                    .Where(j => j.Status == JobStatus.Completed || j.Status == JobStatus.Failed)
                    .ToList();

                foreach (var job in completedJobs)
                {
                    _jobs.Remove(job);
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            throw;
        }
    }

    public void CancelAll()
    {
        try
        {
            lock (_lock)
            {
                foreach (var job in _jobs.Where(j => j.Status == JobStatus.Pending))
                {
                    job.Status = JobStatus.Canceled;
                    OnJobStatusChanged(job);
                }
            }
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
            throw;
        }
    }

    protected virtual void OnJobStatusChanged(TranscriptionJob job)
    {
        try
        {
            JobStatusChanged?.Invoke(this, new JobEventArgs { Job = job });
        }
        catch (Exception ex)
        {
            Utilities.WriteToLog(ex);
        }
    }
}
