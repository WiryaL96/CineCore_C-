using CineCore.ViewModels.Base; // Wajib panggil ini bang

namespace CineCore.Models
{
    // WAJIB nambahin : ViewModelBase di sini
    public class Seat : ViewModelBase
    {
        public string Row { get; set; } = string.Empty;
        public int Column { get; set; }

        // Label ini yang bakal nampilin "A1", "B2", dll di kursi
        public string Label => $"{Row}{Column}";

        // INI DIA ALARM-NYA! Biar UI tau warnanya harus ganti.
        private SeatState _state;
        public SeatState State
        {
            get => _state;
            set => SetProperty(ref _state, value); // Harus pake SetProperty biar layarnya nge-refresh
        }
    }

    public enum SeatState { Available, Selected, Booked }
}