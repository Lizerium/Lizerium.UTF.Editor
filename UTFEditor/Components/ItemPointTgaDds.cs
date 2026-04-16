namespace UTFEditor.Components
{
    public class ItemPointTgaDds
    {
        /// <summary>
        /// Полное имя файла, включая расширение
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// Чистое имя файла
        /// </summary>
        public string ClearName { get; set; }
        /// <summary>
        /// MIP0, MIP1, ..., либо просто MIPS
        /// </summary>
        public string MipName { get; set; }
    }
}
