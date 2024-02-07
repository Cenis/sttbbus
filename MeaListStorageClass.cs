using System;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Xml;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;
using System.Linq;
using SttBbusCanAnalyzer.src;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SttBbusCanAnalyzer
{
    /// <summary>
    /// This class was only created to get the Data from the CanConNet class because 
    /// of the STAThread exception that couldn't be fixed
    /// </summary>
    public class MeaListStorageClass
    {
        //public MeaListClass meaLists;
        private FileStream meaListFs;
        public MeaListStorageClass()
        {
        }

        public void LoadFromBDV(string Path, TabToInitialize newTabInstance)
        {
            newTabInstance.meaListClass.MeaListVT1.Clear();
            newTabInstance.meaListClass.MeaListVT2.Clear();

            IEnumerable<XElement> lineMD30Elems;
            IEnumerable<XElement> meaElems;

            try
            {
                XElement Test = XElement.Load(Path);

                //Get a list of lineMD30Elements
                lineMD30Elems = Test.Descendants("Element")
                     .Where(eee => eee.Attributes("technicalName").Any(a => a.Value == "LineMD30Elem"));

                //Get the property Element containing the VT name
                XElement vtElem = lineMD30Elems.ElementAt(0).Elements("Property")
                     .Where(eee => eee.Attributes("technicalName").Any(a => a.Value == "customerText")).First();

                //select the attribute value and put its value into a string
                XAttribute vtAttr = vtElem.Attributes("value").FirstOrDefault();
                string vtName = vtAttr.Value;
                Console.WriteLine(vtName);


                meaElems = lineMD30Elems.ElementAt(0).Elements("Element");

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw new Exception();
            }

            
            string meaName;
            string meaAddr;

            
            void ReadMeaLists(XElement meaElem, byte vtNumber)
            {
                //Get MEA name, which is directly an attribute value of the element
                meaName = meaElem.Attributes("localizedName").First().Value;
                //Get MEA address, which is an attribute value of a child element property
                XElement meaAddrElem = meaElem.Elements("Property")
                  .Where(eee => eee.Attributes("technicalName").Any(a => a.Value == "address")).First();
                meaAddr = meaAddrElem.Attributes("value").First().Value;
                Console.WriteLine(meaName + ", Address: " + meaAddr);
                MeaElem ElementToAdd = new(MeaElem.MEAType_Str2Num(meaName));// new MeaElem();
                ElementToAdd.MEA_Addr = Convert.ToByte(meaAddr, CultureInfo.InstalledUICulture);
                ElementToAdd.MEA_TypeNum = MeaElem.MEAType_Str2Num(meaName);
                ElementToAdd.MEA_TypeStr = meaName;
                // TODO: check whether Default is correct. 
                ElementToAdd.ConAlias = newTabInstance.canAnalyzer.vmd.alias;// "Default";
                ElementToAdd.VT = vtNumber;
                if(vtNumber == 1)
                {
                    ElementToAdd.VT = 1;
                    newTabInstance.meaListClass.MeaListVT1.Add(ElementToAdd);
                }
                else
                {
                    ElementToAdd.VT = 2;
                    newTabInstance.meaListClass.MeaListVT2.Add(ElementToAdd);
                }
            }

            foreach (XElement meaElem in meaElems)
            {
                ReadMeaLists(meaElem, 1);
            }

            meaElems = lineMD30Elems.ElementAt(1).Elements("Element");

            foreach (XElement meaElem in meaElems)
            {
                ReadMeaLists(meaElem, 2);
            }

            newTabInstance.meaListClass.MdData = new MeaElem(MeaElem.MEAType_Str2Num("MD"));
            newTabInstance.meaListClass.MdData.MEA_Addr = Convert.ToByte(0, CultureInfo.InstalledUICulture);
            newTabInstance.meaListClass.MdData.MEA_TypeNum = MeaElem.MEAType_Str2Num("MD");
            newTabInstance.meaListClass.MdData.MEA_TypeStr = "MD";
            newTabInstance.meaListClass.MdData.VT = 0;
        }

        public void Save(string Path, TabToInitialize newTabInstance)
        {
            XmlSerializer aSerializer = new(typeof(MeaListClass));
            XmlWriter writer = XmlWriter.Create(Path,
                                                 new XmlWriterSettings
                                                 {
                                                     OmitXmlDeclaration = false,
                                                     Encoding = Encoding.UTF8,
                                                     Indent = true
                                                 });
            writer.WriteStartDocument(true);
            aSerializer.Serialize(writer, newTabInstance.meaListClass);
            writer.Close();

        }

        public void Load(string Path, TabToInitialize newTabInstance)
        {
            try
            {
                XmlSerializer aSerializer = new(typeof(MeaListClass));
                meaListFs = new FileStream(Path, FileMode.Open);
                newTabInstance.meaListClass = (MeaListClass)aSerializer.Deserialize(meaListFs);
                meaListFs.Close();
            }
            catch
            {
                //catch for unit test
            }
        }

        /*
        public ObservableCollection<MeaElem> FindMeaListVT(uint vtId)
        {
            if (vtId == 1)
            {
                return meaLists.MeaListVT1;
            }
            else if (vtId == 2)
            {
                return meaLists.MeaListVT2;
            }
            else
            {
                throw new Exception();
            }
        }

        private MeaElem GetMeaElem(uint vtId, uint meaListIndex)
        {
            if (vtId == 1)
            {
                return meaLists.MeaListVT1[(int)meaListIndex];
            }
            else if (vtId == 2)
            {
                return meaLists.MeaListVT2[(int)meaListIndex];
            }
            else
            {
                throw new Exception();
            }
        }

        public int GetNumberOfMeaElems(uint vtId)
        {

            if (vtId == 1)
            {
                return meaLists.MeaListVT1.Count;
            }
            else if (vtId == 2)
            {
                return meaLists.MeaListVT2.Count;
            }
            else
            {
                throw new Exception();
            }
        }

        public uint GetMeaAddress(uint vtId, uint meaListIndex)
        {
            return GetMeaElem(vtId, meaListIndex).MEA_Addr;
        }

        public uint GetTypeNumber(uint vtId, uint meaListIndex)
        {
            return GetMeaElem(vtId, meaListIndex).MEA_TypeNum;
        }

        public MeaElem FindMeaElem(uint vtId, uint meaAddress)
        {
            if (vtId == 1)
            {
                return meaLists.MeaListVT1.Where(x => x.MEA_Addr == meaAddress).First();
            }
            else if (vtId == 2)
            {
                return (MeaElem)meaLists.MeaListVT2.Where(x => x.MEA_Addr == meaAddress);
            }
            else
            {
                throw new Exception();
            }
        }
        */
    }

    //  [Serializable]
    //  [XmlRoot(ElementName = "MeaListClass")]
    [XmlRoot("MeaListClass", IsNullable = false)]
    public class MeaListClass
    {
        [XmlArray("MeaListVT1")]
        [XmlArrayItem("MeaElem")]
        public BindingList<MeaElem> MeaListVT1 { get; }

        [XmlArray("MeaListVT2")]
        [XmlArrayItem("MeaElem")]
        public BindingList<MeaElem> MeaListVT2 { get; }

        public MeaElem MdData;

        public MeaListClass()
        {
            MdData = new MeaElem(255);
            MeaListVT1 = new BindingList<MeaElem>();
            MeaListVT2 = new BindingList<MeaElem>();
        }

        public MeaElem GetMeaElem(uint vtId, uint meaListIndex)
        {
            if (vtId == 1)
            {
                return MeaListVT1[(int)meaListIndex];
            }
            else if (vtId == 2)
            {
                return MeaListVT2[(int)meaListIndex];
            }
            else
            {
                return null;
                //throw new Exception();
            }
        }
    }
}
