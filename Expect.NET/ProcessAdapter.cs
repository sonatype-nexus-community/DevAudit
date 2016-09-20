
namespace ExpectNet
{
    class ProcessAdapter : IProcess
    {
        private System.Diagnostics.Process process;

        public ProcessAdapter(System.Diagnostics.Process process)
        {
            this.process = process;
        }

        public System.Diagnostics.ProcessStartInfo StartInfo
        {
            get { return process.StartInfo; }
        }

        public System.IO.StreamReader StandardOutput
        {
            get { return process.StandardOutput; }
        }

        public System.IO.StreamReader StandardError
        {
            get { return process.StandardError; }
        }

        public System.IO.StreamWriter StandardInput
        {
            get { return process.StandardInput; }
        }

        public void Start()
        {
            process.Start();
        }
    }
}
