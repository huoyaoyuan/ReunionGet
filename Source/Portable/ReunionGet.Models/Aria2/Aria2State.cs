using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Models.Aria2
{
    public class Aria2State
    {
        private readonly Aria2Host _host;
        public Aria2State(Aria2Host host) => _host = host;

        private readonly Dictionary<Aria2GID, Aria2Task> _tasksDict
            = new Dictionary<Aria2GID, Aria2Task>();

        public IReadOnlyCollection<Aria2GID> TrackedGIDs => _tasksDict.Keys;
        public IReadOnlyCollection<Aria2Task> AllTasks => _tasksDict.Values;
        public IEnumerable<Aria2Task> TopLevelTasks => _tasksDict.Values.Where(x => x.IsTopLevel);

        internal void PostAllTrackedRefresh(IEnumerable<DownloadProgressStatus> status)
        {
            foreach (var s in status)
            {
                var task = _tasksDict[s.Gid];
                task.Load(s);
                foreach (var followedGID in s.FollowedBy ?? Enumerable.Empty<Aria2GID>())
                {
                    if (!_tasksDict.ContainsKey(followedGID))
                    {
                        var followedTask = new Aria2Task(_host, followedGID, task);
                        _tasksDict.Add(followedGID, followedTask);
                        AnyTrackedTaskAdded?.Invoke(followedTask);
                        task.AddFollowedTask(followedTask);
                    }
                }
            }
        }

        public event Action<Aria2Task>? TopLevelTaskAdded;
        public event Action<Aria2Task>? AnyTrackedTaskAdded;

        public async Task<Aria2Task> AddMangetTaskAsync(string magnet)
        {
            var gid = await _host.Connection.AddUriAsync(magnet).ConfigureAwait(false);
            var task = new Aria2Task(_host, gid);
            _tasksDict.Add(gid, task);
            TopLevelTaskAdded?.Invoke(task);
            AnyTrackedTaskAdded?.Invoke(task);
            return task;
        }
    }
}
