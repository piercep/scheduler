using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Scheduler4.Interfaces;
using ThreadSafeCollections;

namespace Scheduler4.Handlers
{
    /// <summary>
    /// Scheduler for handling the long delayed work items
    /// </summary>
    internal class SnailScheduler : ISchedulerHandler
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
        /// list of items to add back to the queue
        /// </summary>
        private readonly TList<IExecutableWorkItem> ReAddList = new TList<IExecutableWorkItem>();

        /// <summary>
        /// list of items to add to the fast queue
        /// </summary>
        private readonly TList<IExecutableWorkItem> FastList = new TList<IExecutableWorkItem>();

        /// <summary>
        /// list of items to add to the slow queue
        /// </summary>
        private readonly TList<IExecutableWorkItem> SlowList = new TList<IExecutableWorkItem>();

        /// <summary>
        /// The item list.
        /// </summary>
        private TList<IExecutableWorkItem> ItemList;

        /// <summary>
        /// The work item total elapsed ms
        /// </summary>
        private double WorkItemTotalMs;

        // Properties
        #endregion

        #region ISchedulerHandler Members

        /// <summary>
        /// When true, this scheduler is attempting to shutdown
        /// </summary>
        public bool WantExit { get; set; }

        /// <summary>
        /// The work items executed count
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
                        // exit if wanting exit
                        if (this.WantExit)
                            return;

                        // get this item's total ms
                        WorkItemTotalMs = new TimeSpan(x.ExecutionTime.Ticks - CurrentTime).TotalMilliseconds;

                        // decide which queue the items go to
                        if (WorkItemTotalMs <= MyScheduleEngine.FastTimeMs)
                            FastList.Add(x);
                        else if (WorkItemTotalMs <= MyScheduleEngine.SlowTimeMs)
                            SlowList.Add(x);
                        else
                            ReAddList.Add(x);
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
                    if (FastList.Count > 0)
                        MyScheduleEngine.FastSched.AddItemsToQueue(FastList.ToList());
                    if (SlowList.Count > 0)
                        MyScheduleEngine.SlowSched.AddItemsToQueue(SlowList.ToList());
                    ReAddList.Clear();
                    FastList.Clear();
                    SlowList.Clear();
                    ItemList.Clear();
                }
            }
        }

        /// <summary>
        /// Associated scheduler engine
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
                {
                    m_SleepTimeMs = ((int)Math.Round(
                        Math.Abs(MyScheduleEngine.SlowTimeMs - MyScheduleEngine.FastTimeMs) /
                            MyScheduleEngine.FastTimeMs) + 3) * MyScheduleEngine.Frequency;
                    if (m_SleepTimeMs < MyScheduleEngine.Frequency)
                        m_SleepTimeMs = MyScheduleEngine.Frequency;
                }

                return m_SleepTimeMs;
            }

            set { m_SleepTimeMs = value; }
        }

        #endregion
    }
}
