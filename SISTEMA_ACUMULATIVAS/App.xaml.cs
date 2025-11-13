using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SISTEMA_ACUMULATIVAS
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            LoginWindow loginWindow = new LoginWindow();

            // Mostramos el Login como un diálogo
            bool? dialogResult = loginWindow.ShowDialog();

            // Si el usuario se logueó con éxito (DialogResult == true)
            if (dialogResult.HasValue && dialogResult.Value)
            {
                // Abrimos la ventana principal del programa
                MainWindow mainWindow = new MainWindow(); // Este será nuestro dashboard

                // --- ¡ESTA ES LA LÍNEA QUE ARREGLA EL PROBLEMA! ---
                // Le decimos a la App que esta es la nueva ventana principal.
                // Esto evita que la aplicación se cierre sola.
                this.MainWindow = mainWindow;

                mainWindow.Show();
            }
            else
            {
                // Si el usuario cerró el login, terminamos la aplicación
                Application.Current.Shutdown();
            }
        }
    }
}