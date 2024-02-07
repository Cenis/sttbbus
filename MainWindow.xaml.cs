using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using Xceed.Wpf.Toolkit;
using static SttBbusCanAnalyzer.MainWindow;
using Window = System.Windows.Window;

namespace SttBbusCanAnalyzer
{
    public partial class MainWindow : Window, IObserver
    {
        System.Windows.Controls.CheckBox[] checkBoxArray;
        System.Windows.Controls.TextBox[] wpfFieldsArray;
        ColorPicker[] colorPickerBackroundArray;

        [XmlIgnore]


        TabToInitialize newTabInstance;

        Dictionary<string, TabToInitialize> aliasToTab = new();
        Dictionary<string, CanAnalyzer> canAnalyzerToTab = new();

        private XElement loadedDocument;
        private List<HardwareItem> hardwareItems;


        public MainWindow()
        {

            InitializeComponent();
            hardwareItems = new List<HardwareItem>();

            CanAnalyzer canAnalyzer = new();

            canAnalyzer.AttachCanFrame(this);

            InitializeComponent();


            newTabInstance = new()
            {
                Header = "Default",
                canAnalyzer = canAnalyzer,
            };


            canAnalyzer.vmd.alias = "Default";
            aliasToTab.Add(canAnalyzer.vmd.alias, newTabInstance);
            canAnalyzerToTab.Add(canAnalyzer.vmd.alias, canAnalyzer);
            TabControlInstance.Items.Add(newTabInstance);
            //canAnalyzer.AttachMeaList(newTabInstance.meaListClass);


            //View version in GUI
            Version appVersion = typeof(MainWindow).Assembly.GetName().Version;
            versioning_field.Content = $"Version number:  {appVersion.Major}.{appVersion.Minor}.{appVersion.Build}.{appVersion.Revision}";

            checkBoxArray = new System.Windows.Controls.CheckBox[] { Checkbox_Filter_1, Checkbox_Filter_2, Checkbox_Filter_3,
                            Checkbox_Filter_4, Checkbox_Filter_5, Checkbox_Filter_6 , Checkbox_Filter_7 };

            wpfFieldsArray = new System.Windows.Controls.TextBox[] { MeaAddrFilter_1, MeaAddrFilter_2, MeaAddrFilter_3, MeaAddrFilter_4, MeaAddrFilter_5
                                                                   , MeaAddrFilter_6, MeaAddrFilter_7, VTFilter_1, VTFilter_2, VTFilter_3, VTFilter_4
                                                                   , VTFilter_5, VTFilter_6, VTFilter_7, MsgTypeFilter_1, MsgTypeFilter_2, MsgTypeFilter_3
                                                                   , MsgTypeFilter_4, MsgTypeFilter_5, MsgTypeFilter_6, MsgTypeFilter_7, IDPositionFilter1, IDPositionFilter2
                                                                   , IDPositionFilter3, IDPositionFilter4, IDPositionFilter5, IDPositionFilter6, IDPositionFilter7
                                                                   , IDAddrmodFilter1, IDAddrmodFilter2, IDAddrmodFilter3, IDAddrmodFilter4, IDAddrmodFilter5
                                                                   , IDAddrmodFilter6, IDAddrmodFilter7, IDTypeFilter1, IDTypeFilter2, IDTypeFilter3, IDTypeFilter4
                                                                   , IDTypeFilter5, IDTypeFilter6, IDTypeFilter7 };

            colorPickerBackroundArray = new ColorPicker[] { ClrPcker_Background1, ClrPcker_Background2, ClrPcker_Background3
                                                 , ClrPcker_Background4, ClrPcker_Background5, ClrPcker_Background6
                                                 , ClrPcker_Background7 };


            try
            {
                XElement savedSettings = XElement.Load("appconfig.xml");

                for (byte i = 0; i < wpfFieldsArray.Length; i++)
                {

                    wpfFieldsArray[i].Text = savedSettings.Element(wpfFieldsArray[i].Name).Value;
                }
                for (byte i = 0; i < checkBoxArray.Length; i++)
                {
                    checkBoxArray[i].IsChecked = Convert.ToBoolean(savedSettings.Element(checkBoxArray[i].Name).Value);
                }


                for (byte i = 0; i < colorPickerBackroundArray.Length; i++)
                {
                    string selectedColorAsString = savedSettings.Element(colorPickerBackroundArray[i].Name).Value;
                    if (selectedColorAsString != "")
                    {
                        colorPickerBackroundArray[i].SelectedColor = (Color)ColorConverter.ConvertFromString(selectedColorAsString);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                canAnalyzer.Load("meaLists.xml", newTabInstance);
                //UpdateMeaVtLists(newTabInstance);
            }
            catch
            {
                Console.WriteLine("Can't Create and Restore MEA List from .xml file");
            }
            //newTabInstance.meaListClass.MdData.ResetInputs();  

        }
        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("CloseTab_Click triggered");  // Debug statement

            // Don't close the tab if there's only one.
            if (TabControlInstance.Items.Count <= 1)
            {
                System.Diagnostics.Debug.WriteLine("Cannot close the last tab");  // Debug statement
                return;
            }

            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
            if (button != null)
            {
                System.Diagnostics.Debug.WriteLine("Button is not null");  // Debug statement

                TabToInitialize tab = button.DataContext as TabToInitialize;
                if (tab != null)
                {
                    System.Diagnostics.Debug.WriteLine("Tab is not null");  // Debug statement


                    TabControlInstance.Items.Remove(tab);

                    // Update visibility of all 'X' buttons
                    UpdateCloseButtonVisibility();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Tab is null");  // Debug statement
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Button is null");  // Debug statement
            }
        }

        private void UpdateCloseButtonVisibility()
        {
            foreach (var item in TabControlInstance.Items)
            {
                TabToInitialize tab = item as TabToInitialize;
                if (tab != null)
                {
                    tab.CloseButtonVisibility = (TabControlInstance.Items.Count > 1) ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }


        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();

            try
            {
                foreach (TabToInitialize tabItem in TabControlInstance.Items)
                {
                    tabItem.canAnalyzer.Save("meaLists.xml", (TabToInitialize)tabItem);
                    tabItem.canAnalyzer.FinalizeApp();
                }

                XmlWriter appConWriter = XmlWriter.Create("appconfig.xml",
                                                            new XmlWriterSettings
                                                            {
                                                                OmitXmlDeclaration = false,
                                                                Encoding = Encoding.UTF8,
                                                                Indent = true
                                                            });
                appConWriter.WriteStartDocument(true);
                appConWriter.WriteStartElement("AppSettings");

                for (byte i = 0; i < wpfFieldsArray.Length; i++)
                {
                    appConWriter.WriteElementString(wpfFieldsArray[i].Name, wpfFieldsArray[i].Text);
                }
                for (byte i = 0; i < checkBoxArray.Length; i++)
                {
                    appConWriter.WriteElementString(checkBoxArray[i].Name, checkBoxArray[i].IsChecked.ToString());
                }
                for (byte i = 0; i < colorPickerBackroundArray.Length; i++)
                {
                    appConWriter.WriteElementString(colorPickerBackroundArray[i].Name, colorPickerBackroundArray[i].SelectedColor.ToString());
                }

                appConWriter.WriteEndElement();
                appConWriter.Close();
            }
            catch
            {
                Console.WriteLine("An error occured while trying to create XMLSettings");
            }

        }

        private UInt32 GetConfOut_BitMask()
        {
            UInt32 bitMask;

            bitMask = Convert.ToUInt32(chk_confOut_1.IsChecked, CultureInfo.InstalledUICulture) |
                Convert.ToUInt32(chk_confOut_2.IsChecked, CultureInfo.InstalledUICulture) << 1 |
                Convert.ToUInt32(chk_confOut_3.IsChecked, CultureInfo.InstalledUICulture) << 2 |
                Convert.ToUInt32(chk_confOut_4.IsChecked, CultureInfo.InstalledUICulture) << 3 |
                Convert.ToUInt32(chk_confOut_5.IsChecked, CultureInfo.InstalledUICulture) << 4 |
                Convert.ToUInt32(chk_confOut_6.IsChecked, CultureInfo.InstalledUICulture) << 5 |
                Convert.ToUInt32(chk_confOut_7.IsChecked, CultureInfo.InstalledUICulture) << 6 |
                Convert.ToUInt32(chk_confOut_8.IsChecked, CultureInfo.InstalledUICulture) << 7;

            return bitMask;
        }


        private UInt32 GetConfInpValue()
        {
            UInt32 inpNr;

            inpNr = Convert.ToUInt32(rad_confIn_1.IsChecked, CultureInfo.InstalledUICulture) * 1 +
                Convert.ToUInt32(rad_confIn_2.IsChecked, CultureInfo.InstalledUICulture) * 2 +
                Convert.ToUInt32(rad_confIn_3.IsChecked, CultureInfo.InstalledUICulture) * 3 +
                Convert.ToUInt32(rad_confIn_4.IsChecked, CultureInfo.InstalledUICulture) * 4 +
                Convert.ToUInt32(rad_confIn_5.IsChecked, CultureInfo.InstalledUICulture) * 5 +
                Convert.ToUInt32(rad_confIn_6.IsChecked, CultureInfo.InstalledUICulture) * 6 +
                Convert.ToUInt32(rad_confIn_7.IsChecked, CultureInfo.InstalledUICulture) * 7 +
                Convert.ToUInt32(rad_confIn_8.IsChecked, CultureInfo.InstalledUICulture) * 8;

            return inpNr;
        }
        public void ClrPcker_Background1_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background1.SelectedColor.ToString());
            VTFilter_1.Background = FilterColor;
            MeaAddrFilter_1.Background = FilterColor;
            MsgTypeFilter_1.Background = FilterColor;
            IDTypeFilter1.Background = FilterColor;
            IDPositionFilter1.Background = FilterColor;
            IDAddrmodFilter1.Background = FilterColor;

        }
        public void ClrPcker_Background2_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background2.SelectedColor.ToString());
            VTFilter_2.Background = FilterColor;
            MeaAddrFilter_2.Background = FilterColor;
            MsgTypeFilter_2.Background = FilterColor;
            IDTypeFilter2.Background = FilterColor;
            IDPositionFilter2.Background = FilterColor;
            IDAddrmodFilter2.Background = FilterColor;
        }
        public void ClrPcker_Background3_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background3.SelectedColor.ToString());
            VTFilter_3.Background = FilterColor;
            MeaAddrFilter_3.Background = FilterColor;
            MsgTypeFilter_3.Background = FilterColor;
            IDTypeFilter3.Background = FilterColor;
            IDPositionFilter3.Background = FilterColor;
            IDAddrmodFilter3.Background = FilterColor;
        }
        public void ClrPcker_Background4_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background4.SelectedColor.ToString());
            VTFilter_4.Background = FilterColor;
            MeaAddrFilter_4.Background = FilterColor;
            MsgTypeFilter_4.Background = FilterColor;
            IDTypeFilter4.Background = FilterColor;
            IDPositionFilter4.Background = FilterColor;
            IDAddrmodFilter4.Background = FilterColor;
        }
        public void ClrPcker_Background5_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background5.SelectedColor.ToString());
            VTFilter_5.Background = FilterColor;
            MeaAddrFilter_5.Background = FilterColor;
            MsgTypeFilter_5.Background = FilterColor;
            IDTypeFilter5.Background = FilterColor;
            IDPositionFilter5.Background = FilterColor;
            IDAddrmodFilter5.Background = FilterColor;
        }
        public void ClrPcker_Background6_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background6.SelectedColor.ToString());
            VTFilter_6.Background = FilterColor;
            MeaAddrFilter_6.Background = FilterColor;
            MsgTypeFilter_6.Background = FilterColor;
            IDTypeFilter6.Background = FilterColor;
            IDPositionFilter6.Background = FilterColor;
            IDAddrmodFilter6.Background = FilterColor;
        }
        public void ClrPcker_Background7_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var converter = new BrushConverter();
            Brush FilterColor = (Brush)converter.ConvertFromString(ClrPcker_Background7.SelectedColor.ToString());
            VTFilter_7.Background = FilterColor;
            MeaAddrFilter_7.Background = FilterColor;
            MsgTypeFilter_7.Background = FilterColor;
            IDTypeFilter7.Background = FilterColor;
            IDPositionFilter7.Background = FilterColor;
            IDAddrmodFilter7.Background = FilterColor;
        }



        private void SimulateMD_Checked(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.CheckBox checkBox = sender as System.Windows.Controls.CheckBox;
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;
            if (checkBox.IsChecked == true)
            {
                selectedTab.canAnalyzer.SimulateMD = true;
            }
            else
            {
                selectedTab.canAnalyzer.SimulateMD = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize newTabInstance = (TabToInitialize)TabControlInstance.SelectedItem;

            try
            {
                FrameworkElement feSource = e.Source as FrameworkElement;

                switch (feSource.Name)
                {
                    case "btnInitDevice":
                        newTabInstance.ConsoleBox = newTabInstance.canAnalyzer.Connect();
                        //Deactivate button after click to avoid errors afterwards
                        btnInitDevice.IsEnabled = false;
                        btnSendStdFrame_UI16.IsEnabled = true;
                        break;

                    case "btnSetMDtoConfigMode":
                        newTabInstance.canAnalyzer.SetMdToConfigMode();
                        break;

                    case "btnSetMEAtoConfigMode":
                        newTabInstance.canAnalyzer.SetMeaToConfigMode();
                        break;

                    case "btnConfigMD_Out":
                        newTabInstance.canAnalyzer.ConfigMD(0x01, GetConfOut_BitMask(), Convert.ToUInt32(txtFunctionNr_Conf.Text, CultureInfo.InstalledUICulture));
                        break;

                    case "btnConfigMD_In":
                        newTabInstance.canAnalyzer.ConfigMD(0x02, GetConfInpValue(), Convert.ToUInt32(txtFunctionNr_Conf.Text, CultureInfo.InstalledUICulture));
                        break;

                    case "btnConfigMD_Loop":
                        newTabInstance.canAnalyzer.ConfigMD(0x03, Convert.ToUInt32(txtNum_MEA_VT1.Text, CultureInfo.InstalledUICulture), Convert.ToUInt32(txtNum_MEA_VT2.Text, CultureInfo.InstalledUICulture));
                        break;

                    case "btnSetMDtoNormalMode":
                        newTabInstance.canAnalyzer.SetMdToNormalMode();
                        break;

                    case "btnSetMEAtoNormalMode":
                        newTabInstance.canAnalyzer.SetMeaToNormalMode();
                        break;

                    case "btnConfigMEA_Type0":
                        newTabInstance.canAnalyzer.ConfigMEA(0, Convert.ToUInt32(txtNum_ConfMEA_T0_VT.Text, CultureInfo.InstalledUICulture), Convert.ToUInt32(txtNum_ConfMEA_T0_Addr.Text, CultureInfo.InstalledUICulture),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T0_B1.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T0_B2.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T0_B3.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T0_B4.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T0_B5.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T0_B6.Text, 16));
                        break;

                    case "btnConfigMEA_Type1":
                        newTabInstance.canAnalyzer.ConfigMEA(1, Convert.ToUInt32(txtNum_ConfMEA_T1_VT.Text, CultureInfo.InstalledUICulture), Convert.ToUInt32(txtNum_ConfMEA_T1_Addr.Text, CultureInfo.InstalledUICulture),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T1_B1.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T1_B2.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T1_B3.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T1_B4.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T1_B5.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T1_B6.Text, 16));
                        break;

                    case "btnConfigMEA_Type2":
                        newTabInstance.canAnalyzer.ConfigMEA(2, Convert.ToUInt32(txtNum_ConfMEA_T2_VT.Text, CultureInfo.InstalledUICulture), Convert.ToUInt32(txtNum_ConfMEA_T2_Addr.Text, CultureInfo.InstalledUICulture),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T2_B1.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T2_B2.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T2_B3.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T2_B4.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T2_B5.Text, 16), (byte)0);
                        break;

                    case "btnConfigMEA_Type3":
                        newTabInstance.canAnalyzer.ConfigMEA(3, Convert.ToUInt32(txtNum_ConfMEA_T3_VT.Text, CultureInfo.InstalledUICulture), Convert.ToUInt32(txtNum_ConfMEA_T3_Addr.Text, CultureInfo.InstalledUICulture),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T3_B1.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T3_B2.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T3_B3.Text, 16), (byte)Convert.ToUInt32(txtNum_ConfMEA_T3_B4.Text, 16),
                                           (byte)Convert.ToUInt32(txtNum_ConfMEA_T3_B5.Text, 16), (byte)0);
                        break;

                    case "btnActivateFunction":
                        newTabInstance.canAnalyzer.FunctionControlActivate(Convert.ToUInt32(txtFunctionNr.Text, CultureInfo.InstalledUICulture));
                        break;

                    case "btnStopFunction":
                        newTabInstance.canAnalyzer.FunctionControlStop(Convert.ToUInt32(txtFunctionNr.Text, CultureInfo.InstalledUICulture));
                        break;

                    case "btnSendStdFrame":
                        try
                        {


                            newTabInstance.canAnalyzer.SendStdFrame(Convert.ToUInt32(txtID_MSG.Text, 16),
                                               Convert.ToUInt32(txtID_DST.Text, CultureInfo.InstalledUICulture),
                                               Convert.ToUInt32(txtID_SRC.Text, CultureInfo.InstalledUICulture),
                                               Convert.ToUInt32(txtID_ADR.Text, CultureInfo.InstalledUICulture),
                                               Convert.ToUInt32(txtID_VT.Text, CultureInfo.InstalledUICulture),
                                               Convert.ToUInt32(txtID_TYPE.Text, CultureInfo.InstalledUICulture),
                                               Convert.ToUInt32(txtID_LT.Text, CultureInfo.InstalledUICulture),
                                               Convert.ToByte(txtID_Data0_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data1_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data2_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data3_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data4_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data5_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data6_MD30.Text, 16),
                                               Convert.ToByte(txtID_Data7_MD30.Text, 16),
                                               Convert.ToByte(txtID_DLC_MD30.Text, 16));
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message == "DLC can't be over 8!")
                            {
                                System.Windows.MessageBox.Show(ex.Message);
                            }
                            else
                            {
                                System.Windows.MessageBox.Show(ex.Message);
                            }
                        }
                        break;

                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Please attach a device or use valid data");
            }

        }

        private void btnClr_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;
            selectedTab.CanDataRows.Clear();
        }

        void BtnToggleInputs_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;
            selectedTab.canAnalyzer.fctToggleInputs();
        }

        void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;

            try
            {
                var txtAddressConditionSmaller1 = Convert.ToInt32(selectedTab.txtAddressOfMEA, CultureInfo.InstalledUICulture) < 1;
                txtAddressConditionSmaller1 |= Convert.ToInt32(selectedTab.txtAddressOfMEA, CultureInfo.InstalledUICulture) > 32;
                if (txtAddressConditionSmaller1)
                {
                    System.Windows.MessageBox.Show("Invalid MEA Address. Please enter a valid value");
                    return;
                }
                var txtAddressConditionNot1 = Convert.ToInt32(selectedTab.txtVTChosen, CultureInfo.InstalledUICulture) != 1;
                txtAddressConditionNot1 &= Convert.ToInt32(selectedTab.txtVTChosen, CultureInfo.InstalledUICulture) != 2;
                if (txtAddressConditionNot1)
                {
                    System.Windows.MessageBox.Show("Invalid VT. Please enter a valid VT");
                    return;
                }
            }
            catch
            {
                System.Windows.MessageBox.Show("Enter Correct Values Please");
                return;
            }


            MeaElem TableItemToAdd = new(MeaElem.MEAType_Str2Num(selectedTab.txtMEATypeChosen));
            TableItemToAdd.VT = Convert.ToByte(selectedTab.txtVTChosen, CultureInfo.InstalledUICulture);
            TableItemToAdd.MEA_Addr = Convert.ToByte(selectedTab.txtAddressOfMEA, CultureInfo.InstalledUICulture);
            TableItemToAdd.MEA_TypeStr = selectedTab.txtMEATypeChosen;
            TableItemToAdd.MEA_TypeNum = MeaElem.MEAType_Str2Num(TableItemToAdd.MEA_TypeStr);
            TableItemToAdd.ConAlias = selectedTab.canAnalyzer.vmd.alias;
            bool ItemExisting = false;
            MeaElem ItemThatExists = new();

            if (selectedTab.txtVTChosen == "1")
            {

                foreach (MeaElem Item in selectedTab.meaListClass.MeaListVT1)
                {
                    if (Item.MEA_Addr == TableItemToAdd.MEA_Addr)
                    {
                        ItemExisting = true;
                        ItemThatExists = Item;
                        break;
                    }
                }
                if (!ItemExisting)
                {
                    selectedTab.meaListClass.MeaListVT1.Add(TableItemToAdd);
                }
                else
                {
                    MessageBoxResult res = System.Windows.MessageBox.Show("Do you really want to Overwrite The existing MEA config?", "Overwrite?", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        selectedTab.meaListClass.MeaListVT1.Remove(ItemThatExists);
                        selectedTab.meaListClass.MeaListVT1.Add(TableItemToAdd);
                    }
                }
            }
            else if (selectedTab.txtVTChosen == "2")
            {
                foreach (MeaElem Item in selectedTab.meaListClass.MeaListVT2)
                {
                    if (Item.MEA_Addr == TableItemToAdd.MEA_Addr)
                    {
                        ItemExisting = true;
                        ItemThatExists = Item;
                        break;
                    }
                }
                if (!ItemExisting)
                {
                    selectedTab.meaListClass.MeaListVT2.Add(TableItemToAdd);
                }
                else
                {
                    MessageBoxResult res = System.Windows.MessageBox.Show("Do you really want to Overwrite The existing MEA config?", "Overwrite?", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                    {
                        selectedTab.meaListClass.MeaListVT2.Remove(ItemThatExists);
                        selectedTab.meaListClass.MeaListVT2.Add(TableItemToAdd);
                    }
                }
            }
        }

        public class BooleanToVisibilityConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is bool && (bool)value)
                    return Visibility.Visible;
                else
                    return Visibility.Collapsed;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is Visibility && (Visibility)value == Visibility.Visible)
                    return true;
                else
                    return false;
            }
        }

        private void BtnRemoveLine_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;

            MeaElem TableItemToAdd = new(MeaElem.MEAType_Str2Num(selectedTab.txtMEATypeChosen));
            TableItemToAdd.VT = Convert.ToByte(selectedTab.txtVTChosen, CultureInfo.InstalledUICulture);
            TableItemToAdd.MEA_Addr = Convert.ToByte(selectedTab.txtAddressOfMEA, CultureInfo.InstalledUICulture);
            TableItemToAdd.MEA_TypeStr = selectedTab.txtMEATypeChosen;
            TableItemToAdd.MEA_TypeNum = MeaElem.MEAType_Str2Num(TableItemToAdd.MEA_TypeStr);
            bool ItemExisting = false;
            MeaElem ItemThatExists = new();

            if (selectedTab.txtVTChosen == "1")
            {
                foreach (MeaElem Item in selectedTab.meaListClass.MeaListVT1)
                {
                    if (Item.MEA_Addr == TableItemToAdd.MEA_Addr)
                    {
                        ItemExisting = true;
                        ItemThatExists = Item;
                        break;
                    }
                }
                if (ItemExisting)
                {
                    selectedTab.meaListClass.MeaListVT1.Remove(TableItemToAdd);
                }
                else
                {
                    System.Windows.MessageBox.Show("This item doesn't exist");
                }
            }

            else if (selectedTab.txtVTChosen == "2")
            {
                foreach (MeaElem Item in selectedTab.meaListClass.MeaListVT2)
                {
                    if (Item.MEA_Addr == TableItemToAdd.MEA_Addr)
                    {
                        ItemExisting = true;
                        ItemThatExists = Item;
                        break;
                    }
                }
                if (ItemExisting)
                {
                    selectedTab.meaListClass.MeaListVT2.Remove(TableItemToAdd);
                }
                else
                {
                    System.Windows.MessageBox.Show("This item doesn't exist");
                }
            }
            else
            {
                System.Windows.MessageBox.Show("No Item chosen");
            }
        }

        private void BtnImportConfig_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*"
            };
            // Corrected: use the instance 'openFileDialog' to call ShowDialog()
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    loadedDocument = XElement.Load(openFileDialog.FileName);
                    ExtractHardwareItems();

                    var groups = hardwareItems.GroupBy(item => item.VMD).ToList();

                    foreach (var group in groups)
                    {
                        TabItem newTab = new TabItem
                        {
                            Header = group.Key,
                            Content = new DataGrid
                            {
                                AutoGenerateColumns = true,
                                ItemsSource = group.ToList()
                            }
                        };
                        TabControlInstance.Items.Add(newTab);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error loading XML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        //private void LoadVmdConfigurationsAndCreateTabs(string xmlFilePath)
        //{
        //    // Load the XML document
        //    XElement xmlContent = XElement.Load(xmlFilePath);

        //    // Assuming the XML structure and extracting VMD configurations
        //    var vmdConfigs = xmlContent.Descendants("VMD")
        //                                .Select(vmd => new
        //                                {
        //                                    Alias = vmd.Attribute("Alias")?.Value,
        //                                    // Extract other VMD-specific configurations as needed
        //                                });

        //    foreach (var config in vmdConfigs)
        //    {
        //        // Create a new CanAnalyzer instance for each VMD
        //        CanAnalyzer canAnalyzer = new CanAnalyzer();
        //        // Optionally, configure your CanAnalyzer with data from 'config'

        //        // Check for duplicate alias and avoid creating a new tab if one exists
        //        if (!aliasToTab.ContainsKey(config.Alias))
        //        {
        //            TabToInitialize newTab = new TabToInitialize
        //            {
        //                Header = config.Alias,
        //                canAnalyzer = canAnalyzer,
        //                // Initialize other properties as needed
        //            };

        //            // Add the new tab to the dictionary and TabControl
        //            Dispatcher.Invoke(() =>
        //            {
        //                aliasToTab.Add(config.Alias, newTab);
        //                canAnalyzerToTab.Add(config.Alias, canAnalyzer);
        //                TabControlInstance.Items.Add(newTab);
        //            });
        //        }
        //    }

        //    // Optionally, select the first newly created tab
        //    if (TabControlInstance.Items.Count > 0)
        //    {
        //        Dispatcher.Invoke(() =>
        //        {
        //            TabControlInstance.SelectedItem = TabControlInstance.Items[0];
        //        });
        //    }
        //}





        private void ExtractHardwareItems()
        {
            int vmdCounter = 1;

            var deviceMD30Elems = loadedDocument.Descendants("Element")
                .Where(elem => elem.Attribute("technicalName")?.Value == "DeviceMD30Elem");

            foreach (var md30Elem in deviceMD30Elems)
            {
                var vmdLabel = $"VMD_{vmdCounter:00}";
                int dctNameCounter = 1;  // Reset DCT name counter for each VMD

                var lineMD30Elems = md30Elem.Descendants("Element")
                    .Where(elem => elem.Attribute("technicalName")?.Value == "LineMD30Elem");

                foreach (var lineElem in lineMD30Elems)
                {
                    var vt = lineElem.Elements("Property")
                        .FirstOrDefault(prop => prop.Attribute("technicalName")?.Value == "customerText")?.Attribute("value")?.Value;

                    var meaElements = lineElem.Elements("Element")
                        .Where(elem => elem.Attribute("technicalName") != null && elem.Attribute("technicalName").Value.StartsWith("DeviceMEA20"));

                    foreach (var meaElem in meaElements)
                    {
                        string meaType = meaElem.Attribute("technicalName")?.Value;
                        int lcCount = 0;
                        int ltCount = 0;

                        if (meaType == "DeviceMEA20IElem")
                        {
                            // Sucht in ChannelPhysControlMonitoringLineMEAElem nach controlLineMode und monitoringLineMode
                            var controlMonitoringElems = meaElem.Elements("Element")
                                .Where(subElem => subElem.Attribute("technicalName")?.Value == "ChannelPhysControlMonitoringLineMEAElem");

                            lcCount = controlMonitoringElems
                                .Count(subElem => subElem.Elements("Property")
                                    .Any(prop => prop.Attribute("technicalName")?.Value == "monitoringLineMode"));

                            ltCount = controlMonitoringElems
                                .Count(subElem => subElem.Elements("Property")
                                    .Any(prop => prop.Attribute("technicalName")?.Value == "controlLineMode"));
                        }
                        else
                        {
                            // Original counting logic for other MEA types
                            lcCount = meaElem.Elements("Element")
                                .Count(subElem => subElem.Attribute("technicalName")?.Value == "ChannelPhysMonitoringLineMEAElem");

                            ltCount = meaElem.Elements("Element")
                                .Count(subElem => subElem.Attribute("technicalName")?.Value == "ChannelPhysControlLineMEAElem");
                        }

                        // Create a new HardwareItem object for this MEA
                        var hardwareItem = new HardwareItem
                        {
                            VMD = vmdLabel,
                            VT = vt,
                            TypeStr = meaElem.Attribute("localizedName")?.Value,
                            Addr = meaElem.Elements("Property")
                                .FirstOrDefault(prop => prop.Attribute("technicalName")?.Value == "address")?.Attribute("value")?.Value,
                            TypeNum = meaElem.Elements("Property")
                                .FirstOrDefault(prop => prop.Attribute("technicalName")?.Value == "hwType")?.Attribute("value")?.Value,
                            LC = lcCount,
                            LT = ltCount,
                            Name = $"DCT_{dctNameCounter++}" // Construct the Name with VMD, VT, and count
                        };

                        // Add this HardwareItem to the list
                        hardwareItems.Add(hardwareItem);
                    }
                }

                vmdCounter++;
            }
        }



        private void CreateTabsForVMDs()
        {
            // Group the hardware items by VMD to create tabs for each
            var groupedByVMD = hardwareItems.GroupBy(h => h.VMD);

            foreach (var vmdGroup in groupedByVMD)
            {
                TabItem vmdTab = new TabItem
                {
                    Header = vmdGroup.Key
                };

                // Create a DataGrid for each VMD
                DataGrid vmdDataGrid = new DataGrid
                {
                    AutoGenerateColumns = true,
                    ItemsSource = vmdGroup.ToList()
                };

                vmdTab.Content = vmdDataGrid;
                TabControlInstance.Items.Add(vmdTab);
            }
        }

        public void LoadDataAndCreateTabs()
        {
            ExtractHardwareItems(); // This populates the hardwareItems list
            CreateTabsForVMDs(); // This creates tabs for each VMD group
        }



        private void btnExtractAndSave_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                XDocument resultDocument = new XDocument();
                XElement databaseElement = new XElement("database");

                foreach (var vmdGroup in hardwareItems.GroupBy(item => item.VMD).OrderBy(g => g.Key))
                {
                    XElement vmdElement = new XElement(vmdGroup.Key);
                    XElement vt1Element = new XElement("VT1");
                    XElement vt2Element = new XElement("VT2");
                    bool vt2HasContent = false;

                    foreach (var vtGroup in vmdGroup.GroupBy(item => item.VT).OrderBy(g => g.Key))
                    {
                        XElement vtElement = vtGroup.Key == "VT2" ? vt2Element : vt1Element;
                        int dctCounter = 1; // Counter for DCT
                        int meaCounter = 1; // Counter for MEA names
                        int meaCounterVT2 = 33; // Special counter for MEA20 in VT2

                        foreach (var item in vtGroup)
                        {
                            XElement meaElement = new XElement("MEA",
                                new XAttribute("type", item.TypeStr),
                                new XAttribute("name", item.VT == "VT2" && item.TypeStr.StartsWith("MEA20") ? $"MEA20_{meaCounterVT2++}" : $"MEA20_{meaCounter++}"));

                            for (int i = 0; i < Math.Max(item.LC, item.LT); i++)
                            {
                                meaElement.Add(new XElement("OUT",
                                    new XAttribute("name", $"DCT_{dctCounter++}"),
                                    new XAttribute("LC_TYPE", "3"),
                                    new XAttribute("LC", i < item.LC ? "1" : "0"),
                                    new XAttribute("LT_TYPE", "0"),
                                    new XAttribute("LT", i < item.LT ? "1" : "0")));
                            }

                            vtElement.Add(meaElement);
                            if (vtGroup.Key == "VT2") vt2HasContent = true;
                        }
                    }

                    // Add VT1 if it has content
                    if (vt1Element.HasElements)
                    {
                        vmdElement.Add(vt1Element);
                    }

                    // Always add VT2 regardless of content
                    vmdElement.Add(vt2Element);

                    databaseElement.Add(vmdElement);
                }

                resultDocument.Add(databaseElement);

                try
                {
                    resultDocument.Save(saveFileDialog.FileName);
                    System.Windows.MessageBox.Show("Extracted hardware items saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error saving XML: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        public class HardwareItem
        {
            public string VMD { get; set; }
            public string VT { get; set; }
            public string TypeStr { get; set; }
            public string Addr { get; set; }
            public string TypeNum { get; set; }
            public int LC { get; set; }
            public int LT { get; set; }
            public string Name { get; set; }
        }

        private void btnSendStdFram_UI16_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;
                selectedTab.canAnalyzer.SendStdFrame_UI16(Convert.ToUInt32(txtID_MSG_UI16.Text, 16),
                                    Convert.ToUInt32(txtID_DST_UI16.Text, CultureInfo.InstalledUICulture),
                                    Convert.ToUInt32(txtID_SRC_UI16.Text, CultureInfo.InstalledUICulture),
                                    Convert.ToUInt32(txtID_TYPE_UI16.Text, CultureInfo.InstalledUICulture),
                                    Convert.ToUInt32(txtID_ADDR_UI16.Text, CultureInfo.InstalledUICulture),
                                    Convert.ToUInt32(txtID_POSITION_UI16.Text, CultureInfo.InstalledUICulture),
                                    Convert.ToByte(txtID_DLC_UI16.Text, CultureInfo.InstalledUICulture),
                                    Convert.ToByte(txtID_Data0_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data1_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data2_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data3_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data4_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data5_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data6_UI16.Text, 16),
                                    Convert.ToByte(txtID_Data7_UI16.Text, 16)
                                    );
            }
            catch (Exception ex)
            {
                if (ex.Message == "DLC can't be over 8!")
                {
                    System.Windows.MessageBox.Show(ex.Message);
                }
                else
                {
                    System.Windows.MessageBox.Show("Only numeric values allowed!");
                }
            }
        }



        public static bool UICheckBoxValue = false;
        ////Changes column header
        private void ChangeToUI16(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;

            UICheckBoxValue = chk_UI_messages.IsChecked.Value;
            selectedTab.canAnalyzer.IsUI16 = UICheckBoxValue;

            if (UICheckBoxValue)
            {
                //Visibility of the column header
                MD30FilterGroup.Visibility = Visibility.Collapsed;
                UI16FilterGroup.Visibility = Visibility.Visible;
                //    //Visibility of the text boxes
                //    DataGridColumn_LT.Visibility = Visibility.Collapsed;
                //    DataGridColumn_MEA_Type.Visibility = Visibility.Collapsed;
                //    DataGridColumn_VT.Visibility = Visibility.Collapsed;
                //    DataGridColumn_MEA_Addr.Visibility = Visibility.Collapsed;
                //    DataGridColumn_ID_Addrmod.Visibility = Visibility.Visible;
                //    DataGridColumn_ID_Position.Visibility = Visibility.Visible;
                //    DataGridColumn_ID_Type.Visibility = Visibility.Visible;
                //    //Enable btnSendStdFrame_UI16
                //    UI16Tab.IsSelected = true;

            }
            else
            {
                //    //Visibility of the column header
                MD30FilterGroup.Visibility = Visibility.Visible;
                UI16FilterGroup.Visibility = Visibility.Collapsed;

                //    //Visibility of the text boxes
                //    DataGridColumn_LT.Visibility = Visibility.Visible;
                //    DataGridColumn_MEA_Type.Visibility = Visibility.Visible;
                //    DataGridColumn_VT.Visibility = Visibility.Visible;
                //    DataGridColumn_MEA_Addr.Visibility = Visibility.Visible;
                //    DataGridColumn_ID_Addrmod.Visibility = Visibility.Collapsed;
                //    DataGridColumn_ID_Position.Visibility = Visibility.Collapsed;
                //    DataGridColumn_ID_Type.Visibility = Visibility.Collapsed;

            }

        }

        public void WriteIntoDataGrid(CanDataRow data)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                TabToInitialize newTabInstance = null;
                if (aliasToTab.ContainsKey(data.ConAlias))
                {
                    newTabInstance = aliasToTab[data.ConAlias];
                    newTabInstance.CanDataRows.Add(data);
                    newTabInstance.ConsoleBox = canAnalyzerToTab[data.ConAlias].BusstateTxt;
                }
            });
        }

        public void UpdateMeaGrid(MeaElem meaData)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                TabToInitialize conAliasTab = null;
                if (aliasToTab.ContainsKey(meaData.ConAlias))
                {
                    conAliasTab = aliasToTab[meaData.ConAlias];
                    if (meaData.VT == 1)
                    {
                        int index = conAliasTab.meaListClass.MeaListVT1.IndexOf(conAliasTab.meaListClass.MeaListVT1.Where(x => x.MEA_Addr == meaData.MEA_Addr).First());
                        conAliasTab.meaListClass.MeaListVT1[index] = meaData;
                    }
                    else
                    {
                        int index = conAliasTab.meaListClass.MeaListVT2.IndexOf(conAliasTab.meaListClass.MeaListVT2.Where(x => x.MEA_Addr == meaData.MEA_Addr).First());
                        conAliasTab.meaListClass.MeaListVT2[index] = meaData;
                    }
                }
            });
        }

        public MeaElem FindMeaElem(MeaElem meaElem)
        {
            TabToInitialize conAliasTab = null;
            if (aliasToTab.ContainsKey(meaElem.ConAlias))
            {
                conAliasTab = aliasToTab[meaElem.ConAlias];
                IEnumerable<MeaElem> collection;
                if (meaElem.VT == 1)
                {
                    collection = conAliasTab.meaListClass.MeaListVT1.Where(x => x.MEA_Addr == meaElem.MEA_Addr);
                }
                else
                {
                    collection = conAliasTab.meaListClass.MeaListVT2.Where(x => x.MEA_Addr == meaElem.MEA_Addr);
                }
                if (collection.Count() > 0)
                {
                    return collection.First();
                }
            }
            return null;
        }

        private void btnExportCanData_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;
            SaveFileDialog svd = new();
            try
            {
                if (selectedTab.CanDataRows.Count != 0 && selectedTab.CanDataRows != null)
                {
                    svd.Filter = "CSV file (*.csv) | *.csv";
                    svd.ShowDialog();
                    svd.Dispose();
                    if (!File.Exists(svd.FileName))
                    {
                        File.Create(svd.FileName).Dispose();
                    }
                    selectedTab.SaveCanDataRows(svd.FileName);
                    System.Windows.MessageBox.Show("CAN data successfully saved");
                }
                else
                {
                    System.Windows.MessageBox.Show("No CAN data detected, can't save");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private void btnImportCanData_Click(object sender, RoutedEventArgs e)
        {
            TabToInitialize selectedTab = (TabToInitialize)TabControlInstance.SelectedItem;
            try
            {
                OpenFileDialog ofd = new();
                ofd.Filter = "CSV, TXT or LOG Files (*.csv; *.txt; *.log) | *.csv; *.txt; *.log";
                ofd.RestoreDirectory = true;
                if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (File.Exists(ofd.FileName))
                    {
                        string path = ofd.FileName;

                        if (path.Contains(".csv"))
                        {
                            StreamReader reader = new(File.OpenRead(path));
                            selectedTab.CanDataRows.Clear();
                            selectedTab.ConsoleBox = "";
                            selectedTab.canAnalyzer.ImportCanDataRows(reader);
                            reader.Close();
                        }
                        else if (path.Contains(".txt") || path.Contains(".log"))
                        {
                            StreamReader reader = new(File.OpenRead(path));
                            selectedTab.CanDataRows.Clear();
                            selectedTab.ConsoleBox = "";
                            selectedTab.canAnalyzer.ImportCanDataFromProtectionInterfaceLog(reader);
                            reader.Close();
                        }
                        else
                        {
                            throw new Exception("Your file doesnt match the appropriate format");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }


        private void btnOpenVirtualMdConnectionWindow_Click(object sender, RoutedEventArgs e)
        {
            CanAnalyzer canAnalyzer = new();
            canAnalyzer.AttachCanFrame(this);
            VirtualMdConnectDialog virtualMdConnectDialog = new(canAnalyzer);
            virtualMdConnectDialog.ShowDialog();
            btnConnectVirtualMd.Content = "Open new Tab";

            canAnalyzer.CanCon.IsConnectedWithVMD = true;

            try
            {
                TabToInitialize myTab = new()
                {
                    Header = canAnalyzer.vmd.alias,
                    canAnalyzer = canAnalyzer,
                };

                if (TabControlInstance.Items.Count == 1 && myTab.Header == "Default")
                {
                    aliasToTab.Remove("Default");
                    //Remove tab
                    TabControlInstance.Items.Remove(TabControlInstance.Items[0]);
                }

                aliasToTab.Add(myTab.Header, myTab);
                canAnalyzerToTab.Add(myTab.Header, canAnalyzer);
                TabControlInstance.Items.Add(myTab);
                TabControlInstance.SelectedItem = myTab;
            }
            catch
            {
                System.Windows.MessageBox.Show("VMD/MD with the same Alias already exists");
            }
        }

        private void CanDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var grid = sender as DataGrid;
            if (grid == null || grid.Items == null || grid.Items.Count == 0) // Ensure grid and its items are not null and that it contains items
                return;

            if (chk_AutoScroll.IsChecked == true) // Use == true to safely handle possible null value
            {
                grid.Dispatcher.Invoke(() =>
                {
                    grid.ScrollIntoView(grid.Items[grid.Items.Count - 1]); // Access the last item safely
                });
            }

            if (chk_UI_messages.IsChecked == true)
            {
                // Check if the column indices are within the bounds
                if (grid.Columns.Count > 13)
                {
                    grid.Columns[6].Visibility = Visibility.Collapsed;
                    grid.Columns[8].Visibility = Visibility.Collapsed;
                    grid.Columns[9].Visibility = Visibility.Collapsed;
                    grid.Columns[10].Visibility = Visibility.Collapsed;
                    grid.Columns[11].Visibility = Visibility.Visible;
                    grid.Columns[12].Visibility = Visibility.Visible;
                    grid.Columns[13].Visibility = Visibility.Visible;
                }
            }
            else
            {
                // Check if the column indices are within the bounds
                if (grid.Columns.Count > 13)
                {
                    grid.Columns[6].Visibility = Visibility.Visible;
                    grid.Columns[8].Visibility = Visibility.Visible;
                    grid.Columns[9].Visibility = Visibility.Visible;
                    grid.Columns[10].Visibility = Visibility.Visible;
                    grid.Columns[11].Visibility = Visibility.Collapsed;
                    grid.Columns[12].Visibility = Visibility.Collapsed;
                    grid.Columns[13].Visibility = Visibility.Collapsed;
                }
            }
        }
    }

        public class TabToInitialize : INotifyPropertyChanged
    {
        public CanAnalyzer canAnalyzer { get; set; }

        public Visibility CloseButtonVisibility { get; set; }
        private string _meaType;
        private string _consoleText;
        private bool _isCloseButtonVisible;

        public string Header { get; set; }

        public string txtMEATypeChosen
        {
            get
            {
                return _meaType;
            }
            set
            {
                string[] tmpStr = value.Split(':');
                _meaType = tmpStr[1].Trim();
                NotifyPropertyChanged(nameof(txtMEATypeChosen));
            }
        }

        public string ConsoleBox
        {
            get
            {
                return _consoleText;
            }
            set
            {
                _consoleText = value;
                if (value != null)
                {
                    NotifyPropertyChanged(nameof(ConsoleBox));
                }
            }
        }

        public bool IsCloseButtonVisible
        {
            get { return _isCloseButtonVisible; }
            set
            {
                if (_isCloseButtonVisible != value)
                {
                    _isCloseButtonVisible = value;
                    NotifyPropertyChanged(nameof(IsCloseButtonVisible));
                }
            }
        }

        public ObservableCollection<CanDataRow> CanDataRows { get; } = new ObservableCollection<CanDataRow>();
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string txtVTChosen { get; set; }
        public string txtAddressOfMEA { get; set; }
        public MeaListClass meaListClass { get; set; } = new MeaListClass();

        public void SaveCanDataRows(string path)
        {
            if (new FileInfo(path).Length < 10)
            {
                StreamWriter streamWriter = new StreamWriter(path);
                streamWriter.WriteLine(string.Join(";", "timestamp", "id", "data", "MSG_Type", "src", "dst", "MEA_Addr", "vt", "MEA_Type", "lt", "ConAlias", "Details"));
                streamWriter.Close();
            }

            File.AppendAllLines(path, CanDataRows.Select(x => string.Join(";", x.Timestamp, x.Id,
                    x.Data, x.Msg_type, x.Src,
                    x.Dst, x.MEA_Addr, x.VT, x.MEA_Type, x.LT, x.ConAlias, x.Details)));
        }
    }
}

