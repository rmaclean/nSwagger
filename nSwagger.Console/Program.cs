namespace nSwagger.Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var engine = Engine.Run("../../../tests/kiosk.json");

            engine.Wait();

            var config = new Configuration();
            var ns = Generator.Begin(config);
            foreach (var spec in engine.Result)
            {
               ns = Generator.Go(ns, config, spec);
            }

            Generator.End(config, ns, @"C:\Users\v-robmc\Documents\Projects\nSwagger\nSwagger.Console\generated.cs");
        }
    }
}