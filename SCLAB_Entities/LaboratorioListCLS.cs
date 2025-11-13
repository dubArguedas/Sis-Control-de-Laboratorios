using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SCLAB_Entities
{
    public class LaboratorioListCLS
    {

        [JsonPropertyName("laboratorioId")]
        public int LaboratorioId { get; set; }

        [JsonPropertyName("codigoLaboratorio")]
        public string CodigoLaboratorio { get; set; } = string.Empty;

        [JsonPropertyName("ubicacion")]
        public string Ubicacion { get; set; } = string.Empty;

        [JsonPropertyName("capacidad")]
        public int Capacidad { get; set; }

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = string.Empty;

        [JsonPropertyName("fechaRegistro")]
        public DateTime FechaRegistro { get; set; }
    }
}
