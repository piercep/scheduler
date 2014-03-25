using System;
using System.Threading;
using Scheduler4;
using Scheduler4.Interfaces;

namespace ConsoleApplication1
{
    class Program
    {
        /// <summary>
        /// Fastest scheduler time, in milliseconds.
        /// </summary>
        private const int Frequency = 50;
        
        /// <summary>
        /// Entry point
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            ScheduleEngine engine = new ScheduleEngine();

            try
            {
                // start up the queue
                engine.StartEngine(
                    // fastest check is 50ms
                    Frequency,

                    // items with an execution time less than 500ms
                    // will go into the fast queue
                    new TimeSpan(0, 0, 0, 0, 500),

                    // items with an execution time less than 2 seconds
                    // will go into the slow queue
                    // items with more than 2 seconds go in the Snail queue
                    new TimeSpan(0, 0, 2));

                // add items to queue
                for (int j = 0; j < 8000; j++)
                    engine.AddScheduledItem(new clsWorkItem() { ExecutionTime = DateTime.Now.AddMilliseconds(Frequency + j) });

                // wait 
                Thread.Sleep(Frequency * 50);
            }

            catch (Exception excep)
            {
                Console.WriteLine(excep.Message);
            }

            // Press enter to exit
            Console.WriteLine("FINISHED");
            Console.ReadLine();
            engine.StopEngine();
        }
    }

    /// <summary>
    /// The work item to be executed on each scheduled task
    /// NOTE: create a new one for each type of work item you need
    /// </summary>
    public class clsWorkItem : IExecutableWorkItem
    {
        #region IExecutableWorkItem Members

        /// <summary>
        /// Scheduled work is done here when the schedule is ready
        /// </summary>
        public override void Execute()
        {
            DateTime now = DateTime.Now;

            // as a demo, just output the times
            Console.WriteLine("Now: {0}\tExec Time: {1}\tElapsed Time: {2}",
                now.ToLongTimeString(),
                ExecutionTime.ToLongTimeString(),
                new TimeSpan(now.Ticks - ExecutionTime.Ticks).TotalMilliseconds);
        }

        #endregion
    }
}
