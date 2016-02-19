using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DevAudit.AuditLibrary
{
    class Docker
    {
        public static bool GetContainer(string container_id)
        {
            string command = "docker";
            string process_error = "";
            string[] process_output;
            int process_output_lines = 0;
            ProcessStartInfo psi = new ProcessStartInfo(command);
            psi.Arguments = string.Format("inspect {0}", container_id); ;
            psi.CreateNoWindow = true;
            psi.RedirectStandardError = true;
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = psi;
            List<OSSIndexQueryObject> packages = new List<OSSIndexQueryObject>();
            p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_output = e.Data.Split("\n".ToCharArray());
                    process_output_lines += process_output.Count();
                }
            };
            p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!String.IsNullOrEmpty(e.Data))
                {
                    process_error += e.Data + Environment.NewLine;
                }

            };
            try
            {
                p.Start();
            }
            catch (Win32Exception e)
            {
                if (e.Message == "The system cannot find the file specified")
                {
                    throw new Exception("docker is not installed on this computer.", e);
                }

            }
            finally
            {
                p.Dispose();
            }
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();
            p.WaitForExit();
            p.Close();
            return false;
        }
    }


}
