using System;
using System.Collections.Generic;

namespace MusicPractice.SpatialLoop
{
    public interface ILoopEditCommand
    {
        void Execute();
        void Undo();
    }

    public sealed class DelegateLoopEditCommand : ILoopEditCommand
    {
        private readonly Action _execute;
        private readonly Action _undo;

        public DelegateLoopEditCommand(Action execute, Action undo)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _undo = undo ?? throw new ArgumentNullException(nameof(undo));
        }

        public void Execute()
        {
            _execute();
        }

        public void Undo()
        {
            _undo();
        }
    }

    public sealed class LoopEditHistory
    {
        private readonly Stack<ILoopEditCommand> _undo = new Stack<ILoopEditCommand>();
        private readonly Stack<ILoopEditCommand> _redo = new Stack<ILoopEditCommand>();

        public int UndoCount => _undo.Count;
        public int RedoCount => _redo.Count;

        public void Execute(ILoopEditCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.Execute();
            _undo.Push(command);
            _redo.Clear();
        }

        public bool TryUndo()
        {
            if (_undo.Count == 0)
            {
                return false;
            }

            ILoopEditCommand command = _undo.Pop();
            command.Undo();
            _redo.Push(command);
            return true;
        }

        public bool TryRedo()
        {
            if (_redo.Count == 0)
            {
                return false;
            }

            ILoopEditCommand command = _redo.Pop();
            command.Execute();
            _undo.Push(command);
            return true;
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }
    }
}
