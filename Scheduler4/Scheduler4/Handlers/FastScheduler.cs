using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Scheduler4.Interfaces;
using ThreadSafeCollections;

namespace Scheduler4.Handlers
{
    /// <summary>
    /// The Fast scheduler
    /// </summary>
    internal class FastScheduler : ISchedulerHandler
    {
        #region Properties

        /// <summary>
        /// Queue for holding work items
        /// </summary>
        private readonly TQueue<IExecutableWorkItem> WorkItemQ = new TQueue<IExecutableWorkItem>();

        /// <summary>
        /// The current time.
        /// </summary>
        private long CurrentTime;

        /// <summary>
        /// List of items to re-add at the end of the schedule (for pending items)
        /// </summary>
        private readonly TList<IExecutableWorkItem> ReAddList = new TList<IExecutableWorkItem>();

        /// <summary>
        /// List of items in the list
        /// </summary>
        private TList<IExecutableWorkItem> ItemList;

        // Properties
        #endregion

        #region ISchedulerHandler Members

        /// <summary>
        /// When true, this scheduler is attempting to shutdown
        /// </summary>
        public bool WantExit { get; set; }

        /// <summary>
        /// Number of items executed since startup
        /// </summary>
        private long m_WorkItemsExecuted;

        /// <summary>
        /// Number of items executed since startup
        /// </summary>
        public long WorkItemsExecuted
        {
            get { return Thread.VolatileRead(ref m_WorkItemsExecuted); }
            set { Thread.VolatileWrite(ref m_WorkItemsExecuted, value); }
        }

        /// <summary>
        /// Returns the number of items currently scheduled
        /// </summary>
        private volatile int m_ItemsInQueue;

        /// <summary>
        /// Returns the number of items currently scheduled
        /// </summary>
        public int ItemsInQueue
        {
            get { return m_ItemsInQueue; }
            set { m_ItemsInQueue = value; }
        }

        /// <summary>
        /// Adds the work item to the queue
        /// </summary>
        /// <param name="workItem"></param>
        public void AddItemToQueue(IExecutableWorkItem workItem)
        {
            try
            {
                // add item to queue, then increment counter
                WorkItemQ.Enqueue(workItem);
                m_ItemsInQueue++;
            }

            catch (Exception excep)
            {
                MyScheduleEngine.AddExceptionToQueue(excep);
            }
        }

        /// <summary>
        /// Adds a list of items to the queue
        /// </summary>
        /// <param name="workItems"></param>
        public void AddItemsToQueue(List<IExecutableWorkItem> workItems)
        {
            try
            {
                // add the list ot the queue, then increment counter
                WorkItemQ.EnqueueAll(workItems);
                m_ItemsInQueue += workItems.Count;
            }

            catch (Exception excep)
            {
                MyScheduleEngine.AddExceptionToQueue(excep);
            }
        }

        /// <summary>
        /// Processes the queue
        /// </summary>
        public void ProcessQueue()
        {
            try
            {
                // exit if shutting down
                if (this.WantExit)
                    return;

                // get all the items from the queue, then reset counter
                ItemList = WorkItemQ.DequeueAll();
                m_ItemsInQueue = 0;

                // set time
                CurrentTime = DateTime.Now.Ticks;

                // loop through the items and process them
                Parallel.ForEach(ItemList, x =>
                    {
                        try
                        {
                            // if not time yet, add back to the queue
                            if (x.ExecutionTime.Ticks > CurrentTime)
                            {
                                // add the item to the list, then skip this iteration
                                ReAddList.Add(x);
                                return;
                            }

                            // process the item
                            Task.Factory.StartNew(x.Execute);
                            WorkItemsExecuted++;
                        }

                        catch (Exception excep)
                        {
                            MyScheduleEngine.AddExceptionToQueue(excep);
                        }
                    });
            }

            catch (ThreadAbortException)
            {
            }

            catch (Exception excep)
            {
                MyScheduleEngine.AddExceptionToQueue(excep);
            }

            finally
            {
                // add the list of items back to the queue
                if (!this.WantExit)
                {
                    if (ReAddList.Count > 0)
                        WorkItemQ.EnqueueAll(ReAddList);
                    ReAddList.Clear();
                    ItemList.Clear();
                }
            }
        }

        /// <summary>
        /// Holds the reference to the parent schedule engine
        /// </summary>
        public ScheduleEngine MyScheduleEngine { get; set; }

        /// <summary>
        /// Amount of time, in milliseconds, this engine should sleep
        /// </summary>
        private int m_SleepTimeMs;

        /// <summary>
        /// Amount of time, in milliseconds, this engine should sleep
        /// </summary>
        public int SleepTimeMs
        {
            get
            {
                // get the time if we don't have it already
                if (m_SleepTimeMs == 0)
                    m_SleepTimeMs = MyScheduleEngine.Frequency;

                return m_SleepTimeMs;
            }

            set { m_SleepTimeMs = value; }
        }

        #endregion
    }
}
