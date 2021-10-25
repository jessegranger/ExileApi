using ExileCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using static Assistant.Globals;

namespace Assistant
{
    public class State
    {

        // 'Next' defines the default State we will go to when this State is complete.
        // This value is just a suggestion, the real value is what gets returned by OnTick
        public State Next = null;
        // 'Fail' defines the State to go to if there is any kind of exception
        public State Fail = null;

        public State(State next = null) => Next = next;

        // OnEnter gets called once (by a StateMachine) before the first call to OnTick.
        public virtual State OnEnter() => this;

        // OnTick gets called every frame (by a StateMachine), and should return the next State to continue with (usually itself).
        public virtual State OnTick() => this;

        // OnAbort gets called (by a StateMachine), to ask a State to clean up any incomplete work immediately (before returning).
        public virtual void OnAbort() { }

        // A friendly name for the State, the class name by default.
        public virtual string Name => GetType().Name.Split('.').Last();

        // A verbose description of the State, that includes the Name of the next State (if known).
        public override string ToString() => $"{Name}{(Next == null ? "" : " then " + Next.Name)}";

        // You can create a new State using any Func<State, State>
        public static State Create(string label, Func<State, State> func) => new Runner(label, func);
        public static implicit operator State(Func<State, State> func) => new Runner(func);
        public static implicit operator Func<State, State>(State state) => (s) => s.OnTick();

        // A Runner is a special State that uses a Func<State, State> to convert the Func into a class object with a State interface
        public class Runner : State
        {
            readonly Func<State, State> F;
            public Runner(Func<State, State> func) => F = func;
            public override State OnTick() => F(this);
            public override string Name => name;
            private readonly string name = "...";
            public Runner(string name, Func<State, State> func) : this(func) => this.name = name;
        }

        // Connect a sequence of States together using their .Next property
        public static State Series(params State[] states)
        {
            for (int i = 0; i < states.Length - 1; i++)
            {
                states[i].Next = states[i + 1];
            }
            return states?[0];
        }

        public class Machine : State
        {

            // each machine runs any number of states at once (in 'parallel' frame-wise)
            // when a machine is empty, it gets collected by the reaper
            public LinkedList<State> States;

            public State CurrentState => States.FirstOrDefault();

            // public Machine(params Func<State, State>[] states) : this(states.Cast<State>().ToArray()) { }
            public Machine(params State[] states) => States = new LinkedList<State>(states);

            public override string ToString() => string.Join(" while ", States.Select(s => $"({s})")) + (Next == null ? "" : $" then {Next.Name}");

            private static LinkedListNode<T> RemoveAndContinue<T>(LinkedList<T> list, LinkedListNode<T> node)
            {
                LinkedListNode<T> next = node.Next;
                list.Remove(node);
                return next;
            }

            /// <summary>
            /// Clear is a special state that clears all running states from a MultiState.
            /// </summary>
            /// <example>new StateMachine(
            ///   new WalkTo(X),
            ///   new ShootAt(Y, // will walk and shoot at the same time, when shoot is finished, clear the whole machine (cancel the walk)
            ///     new StateMachine.Clear(this)) );
            ///  </example>
            public class Clear : State
            {
                public Clear(State next) : base(next) { }
                public override State OnTick() => Next;
            }

            private Action<string> logDelegate;
            public void EnableLogging(Action<string> logger) => logDelegate = logger;
            public void DisableLogging() => logDelegate = null;
            private void Log(string s) => logDelegate?.Invoke($"{machineTimer.Elapsed} {s}");

            private static Stopwatch machineTimer = new Stopwatch();
            static Machine() => machineTimer.Start();

            public override State OnTick()
            {
                // Each State in the States list will be ticked "in parallel" (all get ticked each frame)
                Stopwatch watch = new Stopwatch();
                watch.Start();
                try
                {
                    LinkedListNode<State> curNode = States.First;
                    while (curNode != null)
                    {
                        State curState = curNode.Value;
                        State gotoState = curState.OnTick();
                        if (gotoState == null)
                        {
                            Log($"State Finished: {curState.Name}.");
                            curNode = RemoveAndContinue(States, curNode);
                            continue;
                        }
                        if (gotoState != curState)
                        {
                            gotoState = gotoState.OnEnter();
                            Log($"State Changed: {curState.Name} to {gotoState.Name}");
                            if (gotoState.GetType() == typeof(Clear))
                            {
                                Abort(except: curState); // call all OnAbort in State, except curState.OnAbort, because it just ended cleanly (as far as it knows)
                                return gotoState.Next ?? Next;
                            }
                            curState.Next = null; // just in case
                            curNode.Value = gotoState;
                        }
                        curNode = curNode.Next; // loop over the whole list
                    }
                }
                finally
                {
                    watch.Stop();
                    // DrawTextAtPlayer($"StateMachine: {GetType().Name} [{string.Join(", ", States.Select((s) => s.ToString()))}] {watch.Elapsed}");
                }
                return States.Count == 0 ? Next : this;
            }
            public void Abort(State except = null)
            {
                foreach (State s in States) if (s != except) s.OnAbort();
                States.Clear();
            }
            public void Add(State state)
            {
                Log($"State Added: {state.Name}");
                States.AddLast(States.Count == 0 ? state.OnEnter() : state);
            }
            public void Remove(State state) => States.Remove(state);
            public void Remove(Type stateType)
            {
                LinkedListNode<State> cur = States.First;
                while (cur != null)
                {
                    cur = cur.Value.GetType() == stateType ? RemoveAndContinue(States, cur) : cur.Next;
                }
            }

            public bool HasState(Type stateType) => States.Any(s => s.GetType() == stateType);
        }

    }

    public class Delay : State
    {
        Stopwatch sw = new Stopwatch();
        readonly uint ms;
        public Delay(uint ms, State next = null) : base(next) => this.ms = ms;
        public override State OnEnter()
        {
            sw.Restart();
            return this;
        }
        public override State OnTick() => sw.ElapsedMilliseconds >= ms ? Next : (this);
        public override string Name => $"Delay({ms})";
    }

    class InputState : State
    {
        protected readonly static InputSimulator input = new InputSimulator();
        protected InputState(State next = null) : base(next) { }
        public override State OnTick() => Next;
    }

    class KeyState : InputState
    {
        public readonly VirtualKeyCode Key;
        protected KeyState(VirtualKeyCode key, State next = null) : base(next) => Key = key;
    }

    class KeyDown : KeyState
    {
        public KeyDown(VirtualKeyCode key, State next = null) : base(key, next) { }
        public override State OnTick()
        {
            input.Keyboard.KeyDown(Key);
            return Next;
        }
    }

    class KeyUp : KeyState
    {
        public KeyUp(VirtualKeyCode key, State next = null) : base(key, next) { }
        public override State OnTick()
        {
            input.Keyboard.KeyUp(Key);
            return Next;
        }
    }

    class PressKey : KeyState
    {
        public PressKey(VirtualKeyCode key, uint duration, State next = null) : base(key,
            new KeyDown(key, new Delay(duration, new KeyUp(key, next))))
        { }

        public override State OnEnter() => Next;
    }

    class MoveMouse : State
    {
        private readonly static InputSimulator input = new InputSimulator();
        private readonly GameWindow Window;
        public readonly float X;
        public readonly float Y;
        public MoveMouse(GameWindow window, float x, float y, State next = null) : base(next)
        {
            Window = window;
            X = x; Y = y;
        }
        public override State OnTick()
        {
            if (Window == null) return Next;
            var w = Window.GetWindowRectangleTimeCache;
            var bounds = Screen.PrimaryScreen.Bounds;
            input.Mouse.MoveMouseTo(
                (w.Left + X) * 65535 / bounds.Width,
                (w.Top + Y) * 65535 / bounds.Height);
            return Next;
        }
    }
    class LeftMouseDown : InputState
    {
        public LeftMouseDown(State next = null) : base(next) { }
        public override State OnTick()
        {
            input.Mouse.LeftButtonDown();
            return Next;
        }
    }

    class LeftMouseUp : InputState
    {
        public LeftMouseUp(State next = null) : base(next) { }
        public override State OnTick()
        {
            input.Mouse.LeftButtonUp();
            return Next;
        }
    }

    class LeftClick : InputState
    {
        public LeftClick(uint duration, State next = null) : base(
            new LeftMouseDown(new Delay(duration, new LeftMouseUp(next))))
        { }
        public override State OnEnter() => Next;
    }

    class LeftClickAt : InputState
    {
        public LeftClickAt(GameWindow window, float x, float y, uint duration, State next = null) : base(
            new MoveMouse(window, x, y, new Delay(duration, new LeftMouseDown(new Delay(duration, new LeftMouseUp(next))))))
        { }
    }

    class RightMouseDown : InputState
    {
        public RightMouseDown(State next = null) : base(next) { }
        public override State OnTick()
        {
            input.Mouse.RightButtonDown();
            return Next;
        }
    }

    class RightMouseUp : InputState
    {
        public RightMouseUp(State next = null) : base(next) { }
        public override State OnTick()
        {
            input.Mouse.RightButtonUp();
            return Next;
        }
    }

    class RightClick : InputState
    {
        public RightClick(uint duration, State next = null) : base(
            new RightMouseDown(new Delay(duration, new RightMouseUp(next))))
        { }
        public override State OnEnter() => Next;
    }

    class RightClickAt : InputState
    {
        public RightClickAt(GameWindow window, float x, float y, uint duration, State next = null) : base(
            new MoveMouse(window, x, y, new Delay(duration, new RightMouseDown(new Delay(duration, new RightMouseUp(next))))))
        { }
    }

}
