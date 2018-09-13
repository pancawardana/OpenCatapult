﻿// Copyright (c) Polyrific, Inc 2018. All rights reserved.

namespace Polyrific.Catapult.Engine.Core
{
    public class TaskRunnerResult
    {
        /// <summary>
        /// Instantiate <see cref="TaskRunnerResult"/>
        /// </summary>
        public TaskRunnerResult()
        {
        }
        
        /// <summary>
        /// Instantiate <see cref="TaskRunnerResult"/>
        /// </summary>
        /// <param name="isSuccess">Is the process success?</param>
        /// <param name="returnValue">Value to return</param>
        public TaskRunnerResult(bool isSuccess, string returnValue)
        {
            IsProcessed = true;
            IsSuccess = isSuccess;
            ReturnValue = returnValue;
        }

        /// <summary>
        /// Instantiate <see cref="TaskRunnerResult"/>
        /// </summary>
        /// <param name="errorMessage">Error message</param>
        /// <param name="stopTheProcess">Skip the next process?</param>
        public TaskRunnerResult(string errorMessage, bool stopTheProcess = true)
        {
            IsProcessed = true;
            IsSuccess = false;
            ErrorMessage = errorMessage;
            StopTheProcess = stopTheProcess;
        }
        
        /// <summary>
        /// Has the task been processed
        /// </summary>
        public bool IsProcessed { get; set; }

        /// <summary>
        /// Is process success?
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Value to return
        /// </summary>
        public string ReturnValue { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Skip the next process?
        /// </summary>
        public bool StopTheProcess { get; set; }
    }
}