using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Models.Aria2
{
    public sealed class Aria2State : IDisposable
    {
        private class Aria2TaskCollection : KeyedCollection<Aria2GID, Aria2Task>
        {
            protected override Aria2GID GetKeyForItem(Aria2Task item) => item.GID;
        }

        private readonly Aria2Connection _connection;
        public Aria2State(Aria2Host host) => _connection = host.Connection;

        private readonly Aria2TaskCollection _tasksDict
            = new Aria2TaskCollection();

        public IEnumerable<Aria2GID> TrackedGIDs => _tasksDict.Select(x => x.GID);
        public IReadOnlyCollection<Aria2Task> AllTasks => _tasksDict;
        public IEnumerable<Aria2Task> TopLevelTasks => _tasksDict.Where(x => x.IsTopLevel);

        public ReaderWriterLockSlim TasksLock { get; } = new ReaderWriterLockSlim();

        public void Dispose() => TasksLock.Dispose();

        internal void PostAllTrackedRefresh(IEnumerable<DownloadProgressStatus> status)
        {
            foreach (var s in status)
            {
                if (!_tasksDict.TryGetValue(s.Gid, out var task))
                    continue;
                task.Load(s);

                foreach (var followedGID in s.FollowedBy ?? Enumerable.Empty<Aria2GID>())
                {
                    if (!_tasksDict.Contains(followedGID))
                    {
                        var followedTask = new Aria2Task(_connection, followedGID, task);

                        using (TasksLock.UseWriteLock())
                        {
                            _tasksDict.Add(followedTask);
                            AnyTrackedTaskAdded?.Invoke(followedTask);
                            task.AddFollowedTask(followedTask);
                        }
                    }
                }
            }
        }

        public event Action<Aria2Task>? TopLevelTaskAdded;
        public event Action<Aria2Task>? AnyTrackedTaskAdded;

        public async Task<Aria2Task> AddMangetTaskAsync(string magnet)
        {
            var gid = await _connection.AddUriAsync(magnet).ConfigureAwait(false);
            var task = new Aria2Task(_connection, gid);

            using (TasksLock.UseWriteLock())
            {
                _tasksDict.Add(task);
                TopLevelTaskAdded?.Invoke(task);
                AnyTrackedTaskAdded?.Invoke(task);
            }

            return task;
        }

        public async Task<Aria2Task> AddTorrentTaskAsync(Stream torrent)
        {
            var gid = await _connection.AddTorrentAsync(torrent,
                options: new Aria2Options
                {
                    Pause = true
                }).ConfigureAwait(false);
            var task = new Aria2Task(_connection, gid);

            using (TasksLock.UseWriteLock())
            {
                _tasksDict.Add(task);
                TopLevelTaskAdded?.Invoke(task);
                AnyTrackedTaskAdded?.Invoke(task);
            }

            return task;
        }
    }
}
