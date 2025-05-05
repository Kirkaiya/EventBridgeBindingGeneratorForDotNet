
using System.Collections.Generic;

namespace EventBridgeBindingGenerator
{
    public class ClassContainer
    {
        public  string ClassName { get; set; }

        public string ClassFileName {  
            get { 
                return $"{ClassName}.cs";
            } 
        }

        public string ClassDefinitionAsString { get; set; }

        public List<ClassContainer> ChildClasses { get; set; } = new List<ClassContainer>();
    }
}