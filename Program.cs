using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Xml.Linq;
using static SearchMethods.Common;
using static SearchMethods.State;

namespace SearchMethods
{
    public class Common
    {
        public static int stateSize = 9;
        public static int sideSize = 3;
        public static string signature = "15-PuzzlesSignature%%";
        public static int maxTime = 20000;

        public enum EZeroDirection
        {
            LeftDirection,
            RightDirection,
            UpDirection,
            DownDirection
        }
    }

    public class State
    {
        public byte[] numbers = new byte[stateSize];

        public State(byte[] nums)
        {
            numbers = nums;
        }

        //Оператор порівняння станів
        public static bool operator ==(State leftState, State rightState)
        {
            for (int i = 0; i < leftState.numbers.Length; i++)
            {
                if (leftState.numbers[i] != rightState.numbers[i])
                {
                    return false;
                }
            }
            return true;
        }

        public static bool operator !=(State leftState, State rightState)
        {
            return !(leftState == rightState);
        }

        // Метод знаходження евристики стану
        public int GetHeuristic()
        {
             int result = 0;

            for (int i = 1; i < numbers.Length; i++)
            {
                var targetCords = GameTile.GetCoordinatesByIndex(i - 1);
                var tileCords = GameTile.GetCoordinatesByIndex(Array.IndexOf(numbers, (byte)i));
                result += Math.Abs(tileCords.xCord - targetCords.xCord) + Math.Abs(tileCords.yCord - targetCords.yCord);
            }

            return result;
        }


        // Метод обчислення кількості інверсій у стані
        public int CountInversions()
        {
            int result = 0;

            for (int i = 0; i < numbers.Length - 1; i++)
            {
                for (int j = i + 1; j < numbers.Length; j++)
                {
                    if (numbers[i] == 0 || numbers[j] == 0)
                    {
                        continue;
                    }
                    if (numbers[i] > numbers[j])
                    {
                        result++;
                    }
                }
            }

            return result;
        }

        // Метод, що визначає, чи має стан розвз'язок
        public bool IsSolvable()
        {
            int inv = CountInversions();

            if (inv % 2 == 0)
            {
                return true;
            }
            return false;
        }

        // Метод, що перевіряє, чи розв'язаний стан
        public static bool IsSolved(byte[] stateNumbers)
        {
            bool isSolved = true;

            for (int i = 1; i < stateSize - 1; i++)
            {
                if (stateNumbers[i] != i + 1)
                {
                    isSolved = false;
                }
            }

            return isSolved;
        }
    }

    public class RBFSState: State
    {
        public EZeroDirection? previousState;
        public int cost;
        public int heuristic;
        public int totalCost;

        public RBFSState(byte[] nums, EZeroDirection? previousState, int cost): base (nums)
        {
            numbers = nums;
            this.previousState = previousState;
            this.cost = cost;
            this.heuristic = GetHeuristic();
            this.totalCost = heuristic + cost;
        }
    }

    public class LDFSState : State
    {
        public EZeroDirection? previousState;

        public LDFSState(byte[] nums, EZeroDirection? previousState) : base(nums)
        {
            numbers = nums;
            this.previousState = previousState;
        }
    }

    public class LDFSMethod
    {
        public (List<EZeroDirection>, int, int, int, int) GetCorrectPath(byte[] state, int limit)
        {
            List<EZeroDirection> correctPath = new List<EZeroDirection>();

            int counter = 0;
            int non = 0;

            correctPath = LDFS(new LDFSState(state, null), limit, correctPath, ref counter, ref non);

            return (correctPath, counter, non, limit, counter);
        }

        private List<EZeroDirection> LDFS(LDFSState state, int limit, List<EZeroDirection> path, ref int counter, ref int non)
        {
            List<EZeroDirection> tempPath = new List<EZeroDirection>(path);
            if (state.previousState != null)
            {
                tempPath.Add(state.previousState.Value);
            }

            if (IsSolved(state.numbers))
            {
                return tempPath;
            }
            else if (limit == 0)
            {
                non++;
                return new List<EZeroDirection>();
            }
            else
            {
                var movedStates = GetMovedStates(state.numbers, state.previousState);

                foreach (var nextState in movedStates)
                {
                    counter++;
                    path = LDFS(new LDFSState(nextState.state, nextState.direction), limit - 1, new List<EZeroDirection>(tempPath), ref counter, ref non);
                    if (path.Count != 0)
                    {
                        return path;
                    }
                }
            }

            return new List<EZeroDirection>();
        }

        private List<(byte[] state, EZeroDirection direction)> GetMovedStates(byte[] currentState, EZeroDirection? direction)
        {
            List<(byte[], EZeroDirection)> movedStates = new List<(byte[], EZeroDirection)>();

            foreach (EZeroDirection dir in Enum.GetValues(typeof(EZeroDirection)))
            {
                switch (dir)
                {
                    case EZeroDirection.LeftDirection:
                        if (direction == EZeroDirection.RightDirection)
                        {
                            continue;
                        }
                        break;
                    case EZeroDirection.RightDirection:
                        if (direction == EZeroDirection.LeftDirection)
                        {
                            continue;
                        }
                        break;
                    case EZeroDirection.UpDirection:
                        if (direction == EZeroDirection.DownDirection)
                        {
                            continue;
                        }
                        break;
                    case EZeroDirection.DownDirection:
                        if (direction == EZeroDirection.UpDirection)
                        {
                            continue;
                        }
                        break;
                }
                var tempPair = GameTile.Move(currentState, dir);
                if (tempPair.isMoved)
                {
                    movedStates.Add((tempPair.numbersState, dir));
                }
            }

            return movedStates;
        }
    }

    public class RBFSMethod
    {
        public (List<EZeroDirection>, int, int, int, int) GetCorrectPath(byte[] state)
        {
            List<EZeroDirection> correctPath = new List<EZeroDirection>();

            int counter = 0;
            List<byte[]> max = new List<byte[]>();
            List<byte[]> non = new List<byte[]>();
            List<byte[]> allStates = new List<byte[]>();

            correctPath = RBFS(new RBFSState(state, null, 0), int.MaxValue, correctPath, ref counter, ref max, ref non, ref allStates).path;

            return (correctPath, counter, non.Count, max.Count, allStates.Count);
        }

        private (List<EZeroDirection> path, int newF) RBFS(RBFSState state, int limit, List<EZeroDirection> path, ref int counter, ref List<byte[]> max, ref List<byte[]> non, ref List<byte[]> states)
        {
            List<EZeroDirection> tempPath = new List<EZeroDirection>(path);
            if (state.previousState != null)
            {
                tempPath.Add(state.previousState.Value);
            }
            
            if (!max.Contains(state.numbers))
            {
                max.Add(state.numbers);
            }

            if (non.Contains(state.numbers))
            {
                non.Remove(state.numbers);
            }

            if (!states.Contains(state.numbers))
            {
                states.Add(state.numbers);
            }

            if (IsSolved(state.numbers))
            {
                return (tempPath, state.totalCost);
            }

            List<(RBFSState state, int val)> successors = new List<(RBFSState, int)>();

            foreach (var nextState in GetMovedStates(state.numbers, state.previousState))
            {
                int f = Math.Max(new RBFSState(nextState.state, nextState.direction, state.cost + 1).totalCost, state.totalCost);
                successors.Add((new RBFSState(nextState.state, nextState.direction, state.cost + 1), f));
                
                if (!states.Contains(nextState.state))
                {
                    states.Add(nextState.state);
                }

                if (!max.Contains(nextState.state))
                {
                    max.Add(nextState.state);
                }
            }

            while (true)
            {
                counter++;
                successors.Sort((x, y) => x.val.CompareTo(y.val));

                var alternative = limit;
                if (successors.Count > 1)
                {
                     alternative = successors[1].val;
                }


                if (successors[0].val > limit)
                {
                    if (!non.Contains(successors[0].state.numbers))
                    {
                        non.Add(successors[0].state.numbers);
                    }

                    var h = state.IsSolvable();

                    if (max.Contains(successors[0].state.numbers))
                    {
                        max.Remove(successors[0].state.numbers);
                    }

                    if (successors.Count > 1 && max.Contains(successors[1].state.numbers))
                    {
                        max.Remove(successors[1].state.numbers);
                    }

                    if (successors.Count > 2 && max.Contains(successors[2].state.numbers))
                    {
                        max.Remove(successors[2].state.numbers);
                    }

                    return (new List<EZeroDirection>(), successors[0].val);
                }

                var result = RBFS(successors[0].state, Math.Min(limit, alternative), new List<EZeroDirection>(tempPath), ref counter, ref max, ref non, ref states);

                path = result.path;

                var tempSuccessor = successors[0];
                tempSuccessor.val = result.newF;
                successors[0] = tempSuccessor;

                if (path.Count != 0)
                {
                    return (path, successors[0].val);
                }

            }
        }

        private List<(byte[] state, EZeroDirection direction)> GetMovedStates(byte[] currentState, EZeroDirection? direction)
        {
            List<(byte[], EZeroDirection)> movedStates = new List<(byte[], EZeroDirection)>();

            foreach (EZeroDirection dir in Enum.GetValues(typeof(EZeroDirection)))
            {
                switch (dir)
                {
                    case EZeroDirection.LeftDirection:
                        if (direction == EZeroDirection.RightDirection)
                        {
                            continue;
                        }
                        break;
                    case EZeroDirection.RightDirection:
                        if (direction == EZeroDirection.LeftDirection)
                        {
                            continue;
                        }
                        break;
                    case EZeroDirection.UpDirection:
                        if (direction == EZeroDirection.DownDirection)
                        {
                            continue;
                        }
                        break;
                    case EZeroDirection.DownDirection:
                        if (direction == EZeroDirection.UpDirection)
                        {
                            continue;
                        }
                        break;
                }
                var tempPair = GameTile.Move(currentState, dir);
                if (tempPair.isMoved)
                {
                    movedStates.Add((tempPair.numbersState, dir));
                }
            }

            return movedStates;
        }
    }

    internal class GameTile
    {
        public (bool isNeighbor, EZeroDirection? direction) ZeroNeighbor = (false, null);
        public int tileIndex;

        //Метод знаходження координат плитки за індексом
        public static (int xCord, int yCord) GetCoordinatesByIndex(int tileIndex)
        {
            return (tileIndex % sideSize, tileIndex / sideSize);
        }

        //Метод знаходження індексу плитки за координатами
        public static int GetIndexByCords(int xCord, int yCord)
        {
            return yCord * sideSize + xCord;
        }

        //Метод переміщення плитки
        public static (bool isMoved, byte[] numbersState) Move(byte[] numbersState, EZeroDirection dir)
        {
            int zeroIndex = 0;

            for (int i = 0; i < numbersState.Length; i++)
            {
                if (numbersState[i] == 0)
                {
                    zeroIndex = i;
                    break;
                }
            }

            var zeroCords = GetCoordinatesByIndex(zeroIndex);

            if ((dir == EZeroDirection.LeftDirection && zeroCords.xCord == 0) ||
                (dir == EZeroDirection.RightDirection && zeroCords.xCord == sideSize - 1) ||
                (dir == EZeroDirection.UpDirection && zeroCords.yCord == 0) ||
                (dir == EZeroDirection.DownDirection && zeroCords.yCord == sideSize - 1)
            )
            {
                return (false, numbersState);
            }

            int tileIndex = 0;
            switch (dir)
            {
                case EZeroDirection.LeftDirection:
                    tileIndex = GetIndexByCords(zeroCords.xCord - 1, zeroCords.yCord);
                    break;
                case EZeroDirection.RightDirection:
                    tileIndex = GetIndexByCords(zeroCords.xCord + 1, zeroCords.yCord);
                    break;
                case EZeroDirection.UpDirection:
                    tileIndex = GetIndexByCords(zeroCords.xCord, zeroCords.yCord - 1);
                    break;
                case EZeroDirection.DownDirection:
                    tileIndex = GetIndexByCords(zeroCords.xCord, zeroCords.yCord + 1);
                    break;
            }

            byte[] newNumbers = new byte[stateSize];
            for (int i = 0; i < stateSize; i++)
            {
                newNumbers[i] = numbersState[i];
            }
            var temp = newNumbers[tileIndex];
            newNumbers[tileIndex] = newNumbers[zeroIndex];
            newNumbers[zeroIndex] = temp;

            return (true, newNumbers);
        }
    }

    class Program
    {
        public static void Main()
        {

            //state = new byte[]{ 
            //    1,3,0,
            //    4,2,6,
            //    7,5,8 };

            var ldfs = new LDFSMethod();
            var rbfs = new RBFSMethod();

            Console.WriteLine("\n\nRBFS\n\n");
            for (int k = 0; k < 20; k++)
            {
                byte[] state = new byte[stateSize];

                do
                {
                    state = new byte[stateSize];

                    byte[] nums = { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
                    for (int i = 0; i < stateSize; i++)
                    {
                        int j = new Random().Next(nums.Length);
                        state[i] = nums[j];

                        nums = nums.Where(n => n != nums[j]).ToArray();
                    }
                } while (!new RBFSState(state, null, 0).IsSolvable());

                var rbfsPath = rbfs.GetCorrectPath(state);

                for (int i = 0; i < 9; i += 3)
                {
                    for (int h = 0; h < 3; h++)
                    {
                        Console.Write(state[h + i] + " ");
                    }
                    Console.Write("\n");
                }
                Console.WriteLine("iterations: " + rbfsPath.Item2);
                //Console.WriteLine("nodes in mem: " + rbfsPath.Item3);
                Console.WriteLine("nodes in mem: " + (rbfsPath.Item1.Count + 1));
                Console.WriteLine("deadend: " + rbfsPath.Item4);
                Console.WriteLine("nodes: " + rbfsPath.Item5);
                Console.Write("\n");

            }


            Console.WriteLine("\n\nLDFS\n\n");
            for (int k = 0; k < 20; k++)
            {
                byte[] state = new byte[stateSize];

                do
                {
                    state = new byte[stateSize];

                    byte[] nums = { 1, 2, 3, 4, 5, 6, 7, 8, 0 };
                    for (int i = 0; i < stateSize; i++)
                    {
                        int j = new Random().Next(nums.Length);
                        state[i] = nums[j];

                        nums = nums.Where(n => n != nums[j]).ToArray();
                    }
                } while (!new LDFSState(state, null).IsSolvable());

                var ldfsPath = ldfs.GetCorrectPath(state, 25);

                var tmpState = state;

                foreach (var dir in ldfsPath.Item1)
                {
                    tmpState = GameTile.Move(tmpState, dir).numbersState;
                }

                if (IsSolved(tmpState))
                {
                    for (int i = 0; i < 9; i += 3)
                    {
                        for (int h = 0; h < 3; h++)
                        {
                            Console.Write(state[h + i] + " ");
                        }
                        Console.Write("\n");
                    }
                    Console.WriteLine("iterations: " + ldfsPath.Item2);
                    //Console.WriteLine("nodes in mem: " + ldfsPath.Item4);
                    Console.WriteLine("nodes in mem: " + (ldfsPath.Item1.Count + 1));
                    Console.WriteLine("deadend: " + ldfsPath.Item3);
                    Console.WriteLine("nodes: " + ldfsPath.Item5);
                    Console.Write("\n");
                }
                else
                {
                    Console.WriteLine("No path found");
                    Console.Write("\n");
                }
            }


            //Console.WriteLine("\n\nRBFS Path\n\n");

            //foreach (var dir in rbfsPath)
            //{
            //    switch (dir)
            //    {
            //        case EZeroDirection.LeftDirection:
            //            Console.WriteLine("Right");
            //            break;
            //        case EZeroDirection.RightDirection:
            //            Console.WriteLine("Left");
            //            break;
            //        case EZeroDirection.UpDirection:
            //            Console.WriteLine("Down");
            //            break;
            //        case EZeroDirection.DownDirection:
            //            Console.WriteLine("Up");
            //            break;
            //    }
            //}

            //var ldfsPath = ldfs.GetCorrectPath(state, 25);

            //Console.WriteLine("\n\nLDFS Path\n\n");

            //foreach (var dir in ldfsPath)
            //{
            //    switch (dir)
            //    {
            //        case EZeroDirection.LeftDirection:
            //            Console.WriteLine("Right");
            //            break;
            //        case EZeroDirection.RightDirection:
            //            Console.WriteLine("Left");
            //            break;
            //        case EZeroDirection.UpDirection:
            //            Console.WriteLine("Down");
            //            break;
            //        case EZeroDirection.DownDirection:
            //            Console.WriteLine("Up");
            //            break;
            //    }
            //}
        }
    }
}
