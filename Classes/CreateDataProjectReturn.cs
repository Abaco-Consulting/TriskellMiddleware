using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Serialization;

namespace TriskellMiddleware.Classes
{
    public class CreateDataProjectReturn
    {
        /// <summary>
        /// is error?
        /// </summary>
        public bool Error { get; set; }

        /// <summary>
        /// Message if error
        /// </summary>
        public string ErrorMsg { get; set; }

        /// <summary>
        /// Message if success
        /// </summary>
        public string SuccessMsg { get; set; }

   /*     /// <summary>
        /// Nome do projeto
        /// </summary>
        public string NameProject { get; set; }

        /// <summary>
        /// Id projeto em Triskell
        /// </summary>
        public string IdProjectTriskell { get; set; } */
    }

    // resposta xml dos pedidos para criação de projeto
  /*  [XmlRoot(ElementName = "dataobject")]
    public class Dataobject
    {

        [XmlElement(ElementName = "name")]
        public int Name { get; set; }

        [XmlElement(ElementName = "result")]
        public string Result { get; set; }

        [XmlElement(ElementName = "error")]
        public string Error { get; set; }

        [XmlElement(ElementName = "dataobjectid")]
        public string DataObjectId { get; set; }
    }

    [XmlRoot(ElementName = "dataobjects")]
    public class Dataobjects
    {

        [XmlElement(ElementName = "dataobject")]
        public Dataobject Dataobject { get; set; }
    } */
}