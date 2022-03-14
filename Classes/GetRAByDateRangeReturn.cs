using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TriskellMiddleware.Classes
{
    public class GetApprovedTimesheetsByDateRangeReturn
    {
        /// <summary>
        /// is error?
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Message if error
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// The list of Timesheet
        /// </summary>
        public List<Timesheet> Timesheets { get; set; }
    }


}

