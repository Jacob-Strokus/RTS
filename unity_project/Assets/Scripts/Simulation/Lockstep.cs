// Deterministic lockstep networking scaffold (no actual socket layer)
using System.Collections.Generic;
namespace FrontierAges.Sim {
    public class LockstepManager {
        public int LocalPlayerIndex { get; }
        public int PlayerCount { get; }
        public int InputDelayTicks { get; } = 2; // configurable
        private Simulator _sim;
        // Per tick, per player command frames
        private struct Frame { public List<Command> Commands; public bool[] Received; }
        private Dictionary<int, Frame> _frames = new Dictionary<int, Frame>(); // key = tick
        private int _nextSendTick; // next future tick we still need to send our input for
        private List<Command> _outgoingScratch = new List<Command>(32);
        // Hash sync
        public struct HashSample { public int Tick; public ulong Hash; }
        private Queue<HashSample> _recentHashes = new Queue<HashSample>();
        private const int MaxHashSamples = 64;
        public delegate void SendFrameDelegate(int futureTick, IList<Command> commands);
        public delegate void BroadcastHashDelegate(int tick, ulong hash);
        private SendFrameDelegate _sendFn; private BroadcastHashDelegate _hashFn;
        public LockstepManager(Simulator sim, int localPlayer, int playerCount, SendFrameDelegate sendFn, BroadcastHashDelegate hashFn, int inputDelay=2){ _sim=sim; LocalPlayerIndex=localPlayer; PlayerCount=playerCount; _sendFn=sendFn; _hashFn=hashFn; if(inputDelay>0) InputDelayTicks=inputDelay; _nextSendTick = sim.State.Tick + InputDelayTicks; }
        // Local input injection API (wraps simulation issuing into future frame)
        public void QueueLocal(CommandType type, int entityId, int tx, int ty){ int execTick = _sim.State.Tick + InputDelayTicks; var cmd = new Command{ IssueTick=execTick, Type=type, EntityId=entityId, TargetX=tx, TargetY=ty }; EnsureFrame(execTick); _frames[execTick].Commands.Add(cmd); }
        private void EnsureFrame(int tick){ if(!_frames.TryGetValue(tick, out var f)){ f = new Frame{ Commands=new List<Command>(8), Received=new bool[PlayerCount] }; _frames[tick]=f; } }
        // Called each tick after sim.Tick() to send local frame if due and process arrival
        public void PostSimTick(){ int current = _sim.State.Tick; // Send our frame for nextSendTick if we haven't yet and it's still in future
            if(_nextSendTick <= current + InputDelayTicks){ if(_frames.TryGetValue(_nextSendTick, out var f)){ _sendFn?.Invoke(_nextSendTick, f.Commands); f.Received[LocalPlayerIndex]=true; _frames[_nextSendTick]=f; } else { EnsureFrame(_nextSendTick); var nf=_frames[_nextSendTick]; _sendFn?.Invoke(_nextSendTick, nf.Commands); nf.Received[LocalPlayerIndex]=true; _frames[_nextSendTick]=nf; } _nextSendTick++; }
            // Hash broadcast
            _hashFn?.Invoke(current, _sim.LastTickHash); _recentHashes.Enqueue(new HashSample{ Tick=current, Hash=_sim.LastTickHash}); while(_recentHashes.Count>MaxHashSamples) _recentHashes.Dequeue();
            // If next execution tick commands are all in, inject into sim queue
            if(_frames.TryGetValue(current+1, out var nextF)){ if(AllReceived(nextF)){ foreach(var c in nextF.Commands){ _sim.ScheduleCommand(c.Type, c.EntityId, c.TargetX, c.TargetY, c.IssueTick); } } }
        }
        public void ReceiveRemoteFrame(int futureTick, int playerIndex, IList<Command> cmds){ EnsureFrame(futureTick); var f=_frames[futureTick]; if(!f.Received[playerIndex]){ f.Received[playerIndex]=true; for(int i=0;i<cmds.Count;i++) f.Commands.Add(cmds[i]); _frames[futureTick]=f; }
        }
        private bool AllReceived(Frame f){ for(int i=0;i<f.Received.Length;i++) if(!f.Received[i]) return false; return true; }
        // Simple divergence check (external can call compare vs authoritative sample)
        public ulong? GetHashAtTick(int tick){ foreach(var hs in _recentHashes) if(hs.Tick==tick) return hs.Hash; return null; }
    }
}
