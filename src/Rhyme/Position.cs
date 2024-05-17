namespace Rhyme
{
    public record Position(int Line, int Start, int End)
    {
        public int Length => End - Start + 1;
        public static readonly Position NonePosition = new(0, 0, 0);

        public static Position FromTo(Position from, Position to)
        {
            return new Position(from.Line, from.Start, to.End);
        }
    };
}
