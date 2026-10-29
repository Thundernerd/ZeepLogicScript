using System.Collections.Generic;
using System.Linq;

namespace TNRD.Zeepkist.LogicScripting.Compiler.Graph;

internal sealed partial class GraphBuilder
{
    /// <summary>
    /// What a builder has placed and wired so far, to take back out whatever comes after it. Errors, Placed Block
    /// References and the placed blocks they reach stay.
    /// </summary>
    private sealed class Snapshot
    {
        private readonly GraphBuilder _builder;
        private readonly int _blockCount;
        private readonly int _wireCount;
        private readonly int _placedWireCount;
        private readonly int _wiresIntoPlacedCount;
        private readonly int _expansions;
        private readonly Dictionary<(BlockNode Block, int Connector), int> _placedInputLines;

        public Snapshot(GraphBuilder builder)
        {
            _builder = builder;
            _blockCount = builder._graph.Blocks.Count;
            _wireCount = builder._graph.Wires.Count;
            _placedWireCount = builder._graph.PlacedWires.Count;
            _wiresIntoPlacedCount = builder._wiresIntoPlaced.Count;
            DeferredWireCount = builder._deferredWires.Count;
            _expansions = builder._expansions;
            _placedInputLines = new Dictionary<(BlockNode Block, int Connector), int>(builder._placedInputLines);
        }

        public int DeferredWireCount { get; }

        public void Restore()
        {
            _builder._graph.RemoveFrom(_blockCount, _wireCount, _placedWireCount);
            _builder._wiresIntoPlaced.RemoveRange(_wiresIntoPlacedCount, _builder._wiresIntoPlaced.Count - _wiresIntoPlacedCount);
            _builder._deferredWires.RemoveRange(DeferredWireCount, _builder._deferredWires.Count - DeferredWireCount);
            _builder._expansions = _expansions;
            foreach (var literal in _builder._literals.Where(l => l.Value.Block.Index >= _blockCount).ToList())
            {
                _builder._literals.Remove(literal.Key);
            }

            _builder._placedInputLines.Clear();
            foreach (var line in _placedInputLines)
            {
                _builder._placedInputLines.Add(line.Key, line.Value);
            }
        }
    }
}
