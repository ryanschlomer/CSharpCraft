namespace CSharpCraft.Clases
{
    //These clases are classes that can be serialized so they can pass to javascript
    public class Position
    {
        public float X {  get; set; }  
        public float Y { get; set; }
        public float Z { get; set; }

        public Position() { }
        public Position(float x, float y, float z) {  X = x; Y = y; Z = z; }
    }
}
