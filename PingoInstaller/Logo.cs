namespace PingoInstaller;

// "PingoOS" em letras grandes feitas só de caracteres ASCII puro (#, espaço) — nada de
// Unicode estendido. A arte em blocos braille usada antes não renderizava no console
// padrão do Windows 10 (a fonte do terminal não cobre esse bloco Unicode, virava "????").
static class Logo
{
    public static readonly string[] Linhas =
    [
        @" ____  _                    ___  ____ ",
        @"|  _ \(_)_ __   __ _  ___  / _ \/ ___|",
        @"| |_) | | '_ \ / _` |/ _ \| | | \___ \",
        @"|  __/| | | | | (_| | (_) | |_| |___) |",
        @"|_|   |_|_| |_|\__, |\___/ \___/|____/ ",
        @"               |___/                   ",
    ];
}
