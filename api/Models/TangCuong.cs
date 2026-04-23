using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models
{
    [Table("bc_tangcuong")]
    public class TangCuong
    {
        public int? Id { get; set; }
        public int diemKeHoachId { get; set; }
        public int khoaId { get; set; }
        public int? soNgay {  get; set; }
        public int? diem {  get; set; }
    }
}
