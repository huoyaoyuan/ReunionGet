using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ReunionGet.Aria2Rpc;
using ReunionGet.Aria2Rpc.Json;
using ReunionGet.Aria2Rpc.Json.Responses;

namespace ReunionGet.Models.Aria2
{
    public class Aria2Task
    {
        private readonly Aria2Connection _connection;

        public Aria2Task(Aria2Connection connection, Aria2GID gid, Aria2Task? following = null)
        {
            _connection = connection;
            GID = gid;
            Following = following;
        }

        public Aria2GID GID { get; }

        // TODO: use MemberNotNullWhen
        public bool Loaded => RpcResponse != null;

        public DownloadProgressStatus? RpcResponse { get; private set; }

        public bool IsTopLevel => Following is null;

        public Aria2Task? Following { get; }

        private List<Aria2Task>? _followedTasks;
        public IReadOnlyList<Aria2Task> FollowedTasks
            => _followedTasks
            ?? (IReadOnlyList<Aria2Task>)Array.Empty<Aria2Task>();

        internal void Load(DownloadProgressStatus rpcResponse)
        {
            RpcResponse = rpcResponse;
            StatusUpdated?.Invoke();
        }

        internal void AddFollowedTask(Aria2Task followedTask)
        {
            _followedTasks ??= new List<Aria2Task>();
            _followedTasks.Add(followedTask);
            FollowedTaskAdded?.Invoke(followedTask);
        }

        public event Action? StatusUpdated;

        public event Action<Aria2Task>? FollowedTaskAdded;

        public Task PauseAsync()
        {
            if (!Loaded)
                throw new InvalidOperationException("The task hasn't been loaded.");

            return _connection.PauseAsync(GID);
        }

        public Task UnpauseAsync()
        {
            if (!Loaded)
                throw new InvalidOperationException("The task hasn't been loaded.");

            return _connection.UnpauseAsync(GID);
        }

        public Task SetFilesAsync(string selectedFiles)
        {
            if (!Loaded)
                throw new InvalidOperationException("The task hasn't been loaded.");

            return _connection.ChangeOptionAsync(GID, new Aria2Options
            {
                SelectFile = selectedFiles
            });
        }
    }
}
