﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TAS.Client.Config.Views.Plugins
{
    /// <summary>
    /// Interaction logic for PluginsView.xaml
    /// </summary>
    public partial class PluginsView : UserControl
    {
        public PluginsView()
        {
            InitializeComponent();
        }

        private void DataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            //if (!(sender is DataGrid dataGrid))
            //    return;

            //dataGrid.SelectedItem = null;
        }
    }
}
