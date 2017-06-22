using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace sieciServ
{

    /**
    Prosty logger bazujący na: http://www.shayanderson.com/c-sharp/c-logger-class-for-log-messages.htm
    
    */
        class Logger
    {
        /**
         * Log file path
         *  
         * @var string
         */
    private static string __log_file_path= @"c:\projektTesty\"+Thread.CurrentThread.ManagedThreadId+".txt" ;

        /**
         * __log_file_path get/set
         */
        public static string filePath
        {
            get { return Logger.__log_file_path; }
            set { if (value.Length > 0) Logger.__log_file_path = value; }
        }

        /**
         * Flush log file contents
         *  
         * @return void
         */
        public static void flush()
        {
            File.WriteAllText(Logger.filePath, string.Empty);
        }

        /**
         * Log message
         *  
         * @param string msg
         * @return void
         */

        public static void WriteLine(string msg)
        {
            if (msg.Length > 0)
            {
                Console.WriteLine(msg+"\n");

                try
                {
                    using (StreamWriter sw = File.AppendText(Logger.filePath))
                    {
                        sw.WriteAsync(DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString() + " " + msg + " \n").GetAwaiter() ;

                        sw.Flush();
                        sw.Close();
                    }
                }
                catch (Exception e)
                {

                    
                }

      

            }
        }
    }
}
