using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Scheduler4.Handlers;
using Scheduler4.Interfaces;
using ThreadSafeCollections;

namespace Scheduler4
{
    /// <summary>
    /// The scheduler engine
    /// </summary>
    public class ScheduleEngine
    {
        #region Variables

        /// <summary>
        /// Queue of exceptions raised during scheduling
        /// </summary>
        private readonly TQueue<Exception> ExceptionQ = new TQueue<Exception>();

        /// <summary>
        /// Fast scheduler
        /// </summary>
        internal readonly FastScheduler FastSched = new FastScheduler();

        /// <summary>
        /// Slow Scheduler
        /// </summary>
        internal readonly SlowScheduler SlowSched = new SlowScheduler();

        /// <summary>
        /// Snail Scheduler
        /// </summary>
        internal readonly SnailScheduler SnailSched = new SnailScheduler();

        /// <summary>
        /// Thread for the fast scheduler
        /// </summary>
        private Thread threadFastSched;

        /// <summary>
        /// Thread for the slow scheduler
        /// </summary>
        private Thread threadSlowSched;

        /// <summary>
        /// thread for hte snail scheduler
        /// </summary>
        private Thread threadSnailSched;

        /// <summary>
        /// When true, a stop has been issued
        /// </summary>
        private bool IsShuttingDown { get; set; }

        /// <summary>
        /// The frequency of the scheduler
        /// </summary>
        internal int Frequency = 100;

        /// <summary>
        /// Fast time in ms
        /// </summary>
        internal double FastTimeMs;

        /// <summary>
        /// Slow time in ms
        /// </summary>
        internal double SlowTimeMs;

        // Variables
        #endregion

        #region Properties

        /// <summary>
        /// Returns a list of all the exceptions raised during scheduling
        /// </summary>
        public List<Exception> Exceptions
        {
            get { return ExceptionQ.DequeueAll().ToList(); }
        }

        /// <summary>
        /// When true, this scheduler has been started
        /// </summary>
        public bool IsRunning { get; set; }

        // Properties
        #endregion

        #region StartEngine

        /// <summary>
        /// Starts the engine
        /// </summary>
        /// <param name="frequency">smallest time, in milliseconds, the scheduler should check. default is 100</param>
        /// <param name="fastTime">when objects are added to the queue, any object less than or equal to this expiration will be added to the fast queue. Example - 500ms</param>
        /// <param name="slowTime">when objects are added to the queue, any object less than or equal to this expiration will be added to the slow queue. Example - 2 seconds</param>
        public void StartEngine(int frequency, TimeSpan fastTime, TimeSpan slowTime)
        {
            // set frequency
            Frequency = frequency;
            FastTimeMs = fastTime.TotalMilliseconds;
            SlowTimeMs = slowTime.TotalMilliseconds;

            // spin up fast scheduler
            FastSched.MyScheduleEngine = this;
            threadFastSched = new Thread(RunThread);
            threadFastSched.Start(FastSched);

            // spin up slow scheduler
            SlowSched.MyScheduleEngine = this;
            threadSlowSched = new Thread(RunThread);
            threadSlowSched.Start(SlowSched);

            // spin up snail scheduler
            SnailSched.MyScheduleEngine = this;
            threadSnailSched = new Thread(RunThread);
            threadSnailSched.Start(SnailSched);

            // set flag
            IsRunning = true;
        }

        // StartEngine
        #endregion

        #region StopEngine

        /// <summary>
        /// Stops the scheduler engine
        /// </summary>
        public void StopEngine()
        {
            try
            {
                IsShuttingDown = true;

                // set shutdown flags
                if ((FastSched != null) && (threadFastSched != null))
                    FastSched.WantExit = true;
                if ((SlowSched != null) && (threadSlowSched != null))
                    SlowSched.WantExit = true;
                if ((SnailSched != null) && (threadSnailSched != null))
                    SnailSched.WantExit = true;

                // stop threads (wait 20 seconds)
                if ((threadFastSched != null) && (threadFastSched.IsAlive))
                    threadFastSched.Join(new TimeSpan(0, 0, 20));
                if ((threadSlowSched != null) && (threadSlowSched.IsAlive))
                    threadSlowSched.Join(new TimeSpan(0, 0, 20));
                if ((threadSnailSched != null) && (threadSnailSched.IsAlive))
                    threadSnailSched.Join(new TimeSpan(0, 0, 20));

                // abort threads if they have not finished yet
                if ((threadFastSched != null) && (threadFastSched.IsAlive))
                    threadFastSched.Abort();
                if ((threadSlowSched != null) && (threadSlowSched.IsAlive))
                    threadSlowSched.Abort();
                if ((threadSnailSched != null) && (threadSnailSched.IsAlive))
                    threadSnailSched.Abort();
            }

            catch (Exception excep)
            {
                AddExceptionToQueue(excep);
            }

            finally
            {
                // set flag
                IsRunning = false;
            }
        }

        // StopEngine
        #endregion

        #region AddExceptionToQueue

        /// <summary>
        /// Adds this exception to the queue
        /// </summary>
        /// <param name="excep"></param>
        internal void AddExceptionToQueue(Exception excep)
        {
            ExceptionQ.Enqueue(excep);
        }

        // AddExceptionToQueue
        #endregion

        #region AddScheduledItem

        /// <summary>
        /// Adds the item to the scheduled queue
        /// </summary>
        /// <param name="workItem"></param>
        public void AddScheduledItem(IExecutableWorkItem workItem)
        {
            try
            {
                // get the execution time in milliseconds
                double TotalMs = new TimeSpan(workItem.ExecutionTime.Ticks - DateTime.Now.Ticks).TotalMilliseconds;

                // need to check execution time before adding to q
                if (TotalMs <= FastTimeMs)
                    FastSched.AddItemToQueue(workItem);
                else if (TotalMs <= SlowTimeMs)
                    SlowSched.AddItemToQueue(workItem);
                else
                    SnailSched.AddItemToQueue(workItem);
            }

            catch (Exception excep)
            {
                AddExceptionToQueue(excep);
            }
        }

        // AddScheduledItem
        #endregion

        #region RunThread

        /// <summary>
        /// Runs the class in a new thread
        /// </summary>
        /// <param name="objHandler"></param>
        private void RunThread(object objHandler)
        {
            // unbox
            ISchedulerHandler handler = objHandler as ISchedulerHandler;

            // loop until we shutdown
            while (! IsShuttingDown)
            {
                try
                {
                    // skip if no handler
                    if (handler == null)
                        continue;

                    // process the queue
                    handler.ProcessQueue();

                    // sleep until next time
                    Thread.Sleep(handler.SleepTimeMs);
                }

                catch (Exception excep)
                {
                    AddExceptionToQueue(excep);
                }
            }
        }

        // RunThread
        #endregion
    }
}
