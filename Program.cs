using System.Reflection.Metadata.Ecma335;
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
            int tileIndex = Array.IndexOf(numbers, (byte)0);
            int row = GameTile.GetCoordinatesByIndex(tileIndex).yCord;
            int inv = CountInversions();

            if (inv % 2 == 0 && row % 2 == 1)
            {
                return true;
            }
            else if (inv % 2 == 1 && row % 2 == 0)
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

        //public static byte[] 
    }
    
    public class LDFSMethod
    {
        public List<EZeroDirection> GetCorrectPath(byte[] state, int limit)
        {
            List<EZeroDirection> correctPath = new List<EZeroDirection>();

            correctPath = LDFS(state, limit, correctPath, null).path;

            return correctPath;
        }

        private (List<EZeroDirection> path, bool isCorrect) LDFS(byte[] state, int limit, List<EZeroDirection> path, EZeroDirection? direction)
        {
            bool isCorrect = false;
            List<EZeroDirection> tempPath = new List<EZeroDirection>(path);
            if (direction != null)
            {
                tempPath.Add(direction.Value);
            }

            if (IsSolved(state))
            {
                isCorrect = true;
                return (tempPath, isCorrect);
            }
            else if (limit == 0)
            {
                return (new List<EZeroDirection>(), false);
            }
            else
            {
                var movedStates = GetMovedStates(state, direction);

                foreach (var nextState in movedStates)
                {
                    (path, isCorrect) = LDFS(nextState.state, limit - 1, new List<EZeroDirection>(tempPath), nextState.direction);
                    if (isCorrect)
                    {
                        return (path, isCorrect);
                    }
                }
            }

            return (new List<EZeroDirection>(), false);
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
        public List<EZeroDirection> GetCorrectPath(byte[] state)
        {
            List<EZeroDirection> correctPath = new List<EZeroDirection>();

            int counter = 0;
            correctPath = RBFS(state, int.MaxValue, correctPath, null, 0, ref counter).path;

            return correctPath;
        }

        private (List<EZeroDirection> path, bool isCorrect, int newF) RBFS(byte[] state, int limit, List<EZeroDirection> path, EZeroDirection? direction, int cost, ref int counter)
        {
            counter++;
            bool isCorrect = false;
            List<EZeroDirection> tempPath = new List<EZeroDirection>(path);
            if (direction != null)
            {
                tempPath.Add(direction.Value);
            }

            if (IsSolved(state))
            {
                return (tempPath, true, new State(state).GetHeuristic() + cost);
            }

            List<(byte[] state, int cost, EZeroDirection direction)> successors = new List<(byte[], int, EZeroDirection)>();

            foreach (var nextState in GetMovedStates(state, direction))
            {
                int f = Math.Max(new State(nextState.state).GetHeuristic() + (cost + 1), new State(state).GetHeuristic() + cost);
                successors.Add((nextState.state, f, nextState.direction));
            }

            while (true)
            {
                successors.Sort((x, y) => x.cost.CompareTo(y.cost));
                var bestState = successors[0];

                var alternative = limit;
                if (successors.Count > 1)
                {
                     alternative = successors[1].cost;
                }


                if (bestState.cost > limit)
                {
                    return (new List<EZeroDirection>(), false, bestState.cost);
                }

                (path, isCorrect, bestState.cost) = RBFS(bestState.state, Math.Min(limit, alternative), new List<EZeroDirection>(tempPath), bestState.direction, cost + 1, ref counter);

                if (isCorrect)
                {
                    return (path, true, bestState.cost);
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
            } while (!new State(state).IsSolvable());            


            var ldfs = new LDFSMethod();
            var rbfs = new RBFSMethod();

            var rbfsPath = rbfs.GetCorrectPath(state);

            Console.WriteLine("\n\nRBFS Path\n\n");

            foreach (var dir in rbfsPath)
            {
                switch (dir)
                {
                    case EZeroDirection.LeftDirection:
                        Console.WriteLine("Right");
                        break;
                    case EZeroDirection.RightDirection:
                        Console.WriteLine("Left");
                        break;
                    case EZeroDirection.UpDirection:
                        Console.WriteLine("Down");
                        break;
                    case EZeroDirection.DownDirection:
                        Console.WriteLine("Up");
                        break;
                }
            }

            var ldfsPath = ldfs.GetCorrectPath(state, 12);

            Console.WriteLine("\n\nLDFS Path\n\n");

            foreach (var dir in ldfsPath)
            {
                switch (dir)
                {
                    case EZeroDirection.LeftDirection:
                        Console.WriteLine("Right");
                        break;
                    case EZeroDirection.RightDirection:
                        Console.WriteLine("Left");
                        break;
                    case EZeroDirection.UpDirection:
                        Console.WriteLine("Down");
                        break;
                    case EZeroDirection.DownDirection:
                        Console.WriteLine("Up");
                        break;
                }
            }
        }
    }
}
