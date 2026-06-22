using SimpleWebDataAdmin.Services;

namespace SimpleWebDataAdmin
{
    public static class AppState
    {
        // Instanca se sada postavlja dinamički iz Login forme ovisno o unesenom URL-u
        public static ApiClient Api { get; set; } = null!;
    }
}