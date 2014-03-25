using System;
using System.Collections.Generic;

namespace Scheduler4.Interfaces
{
    /// <summary>
    /// Handler for the queues
    /// </summary>
    internal interface ISchedulerHandler
    {
        /// <summary>
        /// When true, this scheduler is attempting to shutdown
        /// </summary>
        bool WantExit { get; set; }

        /// <summary>
        /// Number of items executed since startup
        /// </summary>
        long WorkItemsExecuted { get; set; }

        /// <summary>
        /// Returns the number of items currently scheduled
        /// </summary>
        int ItemsInQueue { get; set; }

        /// <summary>
        /// Adds the work item to the queue
        /// </summary>
        /// <param name="workItem"></param>
        void AddItemToQueue(IExecutableWorkItem workItem);

        /// <summary>
        /// Adds the work item to the queue
        /// </summary>
        /// <param name="workItems"></param>
        void AddItemsToQueue(List<IExecutableWorkItem> workItems);

        /// <summary>
        /// Processes the queue
        /// </summary>
        void ProcessQueue();

        /// <summary>
        /// Holds the reference to the parent schedule engine
        /// </summary>
        ScheduleEngine MyScheduleEngine { get; set; }

        /// <summary>
        /// Amount of time, in milliseconds, this engine should sleep
        /// </summary>
        int SleepTimeMs { get; set; }
    }
}