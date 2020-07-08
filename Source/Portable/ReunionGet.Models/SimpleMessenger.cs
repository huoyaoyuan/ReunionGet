using System;
using System.Collections.Generic;
using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Models
{
    public class SimpleMessenger
    {
        public void Post(DownloadProgressStatus[] update) => Updated?.Invoke(update);

        public event Action<IReadOnlyList<DownloadProgressStatus>>? Updated;
    }
}
