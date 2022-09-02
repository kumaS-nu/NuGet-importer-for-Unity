using System.Collections;
using System.Threading.Tasks;

namespace kumaS.NuGetImporter.Editor.Tests
{
    internal static class TaskExtension
    {
        internal static IEnumerator AsEnumerator(this Task task, bool throwException = true)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (throwException && task.IsFaulted)
            {
                throw task.Exception;
            }
        }
    }
}
