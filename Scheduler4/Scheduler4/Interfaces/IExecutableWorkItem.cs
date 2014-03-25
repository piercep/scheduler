using System;

namespace Scheduler4.Interfaces
{
    /// <summary>
    /// An item which can be placed into a scheduled queue for future execution
    /// </summary>
    public abstract class IExecutableWorkItem
    {
        /// <summary>
        /// Executes the current item
        /// </summary>
        public virtual void Execute()
        {
        }

        /// <summary>
        /// Date/time this item should be executed
        /// </summary>
        public DateTime ExecutionTime { get; set; }
    }
}