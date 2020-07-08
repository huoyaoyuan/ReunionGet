using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Models
{
    public class SimpleMessenger
    {
        public void Post(DownloadProgressStatus[] update) => Updated?.Invoke(update);

        public Action<IReadOnlyList<DownloadProgressStatus>>? Updated { get; set; }

        public BlockingCollection<string> Downloads { get; } = new BlockingCollection<string>();
    }
}
