using System;

namespace Spool.Harlowe
{
    partial class BuiltInMacros
    {
        public String CurrentDate() => new String(DateTime.Now.ToString("ddd MMM dd yyyy"));
        public String CurrentTime() => new String(DateTime.Now.ToString("hh:mm tt"));
        public Number MonthDay() => new Number(DateTime.Now.Day);
        public String WeekDay() => new String(DateTime.Now.DayOfWeek.ToString());
    }
}