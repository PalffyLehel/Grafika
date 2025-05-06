using System.Xml;

namespace lab4;

public class ColladaResourceReader
{
    private static readonly string colladaPath = @"../../../Resources/cube.dae";
    
    public ColladaResourceReader()
    {
        using (Stream objStream = File.OpenRead(colladaPath))
        {
            Console.WriteLine("open");
            XmlDocument proba = new XmlDocument();
            proba.Load(objStream);
            XmlNode root = proba.DocumentElement;

            XmlNodeList nodes = proba.SelectNodes("//COLLADA");
            foreach(XmlNode node in root.ChildNodes)
            {
                Console.WriteLine(node.Name);
            }
        }    
            
    }
}
