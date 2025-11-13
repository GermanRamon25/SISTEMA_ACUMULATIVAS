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

            // --- CAMBIO 1: EVITAR CIERRE PREMATURO ---
            // Le decimos a la App: "No te cierres hasta que yo llame a Shutdown()"
            // Esto evita que el cierre de LoginWindow mate la aplicación.
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            LoginWindow loginWindow = new LoginWindow();

            // Mostramos el Login como un diálogo
            bool? dialogResult = loginWindow.ShowDialog();

            // Si el usuario se logueó con éxito (DialogResult == true)
            if (dialogResult.HasValue && dialogResult.Value)
            {
                // (El MessageBox de prueba ya no es necesario)

                // Abrimos la ventana principal del programa
                MainWindow mainWindow = new MainWindow(); // Este será nuestro dashboard

                // --- ¡ESTA ES LA LÍNEA QUE ARREGLA EL PROBLEMA! ---
                // Le decimos a la App que esta es la nueva ventana principal.
                // Esto evita que la aplicación se cierre sola.
                this.MainWindow = mainWindow;

                mainWindow.Show();

                // --- CAMBIO 2: RESTAURAR CIERRE NORMAL ---
                // Ahora que MainWindow es la jefa, volvemos al modo normal:
                // "Ciérrate cuando el usuario cierre esta (MainWindow)".
                Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
            }
            else
            {
                // Si el login falló (el usuario cerró), apagamos explícitamente.
                Application.Current.Shutdown();
            }
        }
    }
}