using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DTM.Test.OMR;
using DTM.Test.OMR.Helpers;

namespace DTM.Test.Demo
{

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:scan-omr:analysis")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn:scan-omr:analysis", IsNullable = false)]
    public partial class pages
    {

        private pagesPage pageField;

        /// <remarks/>
        public pagesPage page
        {
            get
            {
                return this.pageField;
            }
            set
            {
                this.pageField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:scan-omr:analysis")]
    public partial class pagesPage
    {

        private pagesPageRow[] rowField;

        private string idField;

        private string topLeftField;

        private string bottomLeftField;

        private string topRightField;

        private string bottomRightField;

        private System.DateTime startField;

        private System.DateTime stopTimeField;

        private string templateIdField;

        private string outcomeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("row")]
        public pagesPageRow[] row
        {
            get
            {
                return this.rowField;
            }
            set
            {
                this.rowField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topLeft
        {
            get
            {
                return this.topLeftField;
            }
            set
            {
                this.topLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomLeft
        {
            get
            {
                return this.bottomLeftField;
            }
            set
            {
                this.bottomLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topRight
        {
            get
            {
                return this.topRightField;
            }
            set
            {
                this.topRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomRight
        {
            get
            {
                return this.bottomRightField;
            }
            set
            {
                this.bottomRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime start
        {
            get
            {
                return this.startField;
            }
            set
            {
                this.startField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime stopTime
        {
            get
            {
                return this.stopTimeField;
            }
            set
            {
                this.stopTimeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string templateId
        {
            get
            {
                return this.templateIdField;
            }
            set
            {
                this.templateIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string outcome
        {
            get
            {
                return this.outcomeField;
            }
            set
            {
                this.outcomeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:scan-omr:analysis")]
    public partial class pagesPageRow
    {

        private pagesPageRowAggregate[] aggregateField;

        private string idField;

        private string topLeftField;

        private string bottomLeftField;

        private string topRightField;

        private string bottomRightField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("aggregate")]
        public pagesPageRowAggregate[] aggregate
        {
            get
            {
                return this.aggregateField;
            }
            set
            {
                this.aggregateField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topLeft
        {
            get
            {
                return this.topLeftField;
            }
            set
            {
                this.topLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomLeft
        {
            get
            {
                return this.bottomLeftField;
            }
            set
            {
                this.bottomLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topRight
        {
            get
            {
                return this.topRightField;
            }
            set
            {
                this.topRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomRight
        {
            get
            {
                return this.bottomRightField;
            }
            set
            {
                this.bottomRightField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:scan-omr:analysis")]
    public partial class pagesPageRowAggregate
    {

        private pagesPageRowAggregateBubble[] bubbleField;

        private string idField;

        private string topLeftField;

        private string bottomLeftField;

        private string topRightField;

        private string bottomRightField;

        private string rowIdField;

        private byte valueField;

        private string functionField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("bubble")]
        public pagesPageRowAggregateBubble[] bubble
        {
            get
            {
                return this.bubbleField;
            }
            set
            {
                this.bubbleField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topLeft
        {
            get
            {
                return this.topLeftField;
            }
            set
            {
                this.topLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomLeft
        {
            get
            {
                return this.bottomLeftField;
            }
            set
            {
                this.bottomLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topRight
        {
            get
            {
                return this.topRightField;
            }
            set
            {
                this.topRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomRight
        {
            get
            {
                return this.bottomRightField;
            }
            set
            {
                this.bottomRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string rowId
        {
            get
            {
                return this.rowIdField;
            }
            set
            {
                this.rowIdField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string function
        {
            get
            {
                return this.functionField;
            }
            set
            {
                this.functionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:scan-omr:analysis")]
    public partial class pagesPageRowAggregateBubble
    {

        private string idField;

        private string topLeftField;

        private string bottomLeftField;

        private string topRightField;

        private string bottomRightField;

        private string keyField;

        private byte valueField;

        private ushort areaField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topLeft
        {
            get
            {
                return this.topLeftField;
            }
            set
            {
                this.topLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomLeft
        {
            get
            {
                return this.bottomLeftField;
            }
            set
            {
                this.bottomLeftField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string topRight
        {
            get
            {
                return this.topRightField;
            }
            set
            {
                this.topRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string bottomRight
        {
            get
            {
                return this.bottomRightField;
            }
            set
            {
                this.bottomRightField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string key
        {
            get
            {
                return this.keyField;
            }
            set
            {
                this.keyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte value
        {
            get
            {
                return this.valueField;
            }
            set
            {
                this.valueField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort area
        {
            get
            {
                return this.areaField;
            }
            set
            {
                this.areaField = value;
            }
        }
    }


    internal static class Program
    {
        private static void Main(string[] args)
        {
            //var xml = File.ReadAllText(@"D:\projects\ACE CRAFT ENGINE\DTM test\DTM.Test\DTM.Test.Api\OmrMarkEngine\tmp.xml", Encoding.UTF8);
            //var obj = XMLHelper.Deserialize<pages>(xml);

            using (var parser = new ParserHelper(@"D:\projects\ACE CRAFT ENGINE\DTM test\DTM.Test\DTM.Test.Api\OmrMarkEngine\Upload\Census.jpg"))
            {
                parser.Analizy();

                var result = parser.GetResult();

                //TODO:Print here to result

            }

            Console.ReadKey();
        }
    }
}
