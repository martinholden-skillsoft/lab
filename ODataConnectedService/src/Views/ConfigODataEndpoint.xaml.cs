// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using Microsoft.OData.ConnectedService.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.OData.ConnectedService.Views
{
    /// <summary>
    /// Interaction logic for ConfigODataEndpoint.xaml
    /// </summary>
    public partial class ConfigODataEndpoint : UserControl
    {
        public ConfigODataEndpoint()
        {
            InitializeComponent();
        }

        private void CredentialsNeeded_Checked(object sender, RoutedEventArgs e)
        {
            ResetCredentials.IsEnabled = true;
            SaveCredentials.IsEnabled = true;
        }

        private void CredentialsNeeded_Unchecked(object sender, RoutedEventArgs e)
        {
            ResetCredentials.IsEnabled = false;
            SaveCredentials.IsEnabled = false;
        }
    }
}
