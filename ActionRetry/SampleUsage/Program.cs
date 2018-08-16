using ActionRetry;

namespace SampleUsage
{
    class Program
    {
        static void Main(string[] args)
        {
            var retry = new Retry(() =>
            {
                // Action to retry, returns true when completed successfully
                return true;
            });

            while (!retry.Begin()) { }
        }
    }
}
