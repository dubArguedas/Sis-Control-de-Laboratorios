window.reportes = {
    generarPDF: function (titulo, datos, columnas, logoUrl) {
        const { jsPDF } = window.jspdf;
        const doc = new jsPDF();

        // Configuración
        const fecha = new Date().toLocaleDateString();
        const hora = new Date().toLocaleTimeString();
        
        // Cargar logo (si existe)
        if (logoUrl) {
            const img = new Image();
            img.src = logoUrl;
            img.onload = function () {
                doc.addImage(img, 'PNG', 14, 10, 30, 15); // Ajustar posición y tamaño
                generarContenido(doc, titulo, fecha, hora, columnas, datos);
            };
            img.onerror = function () {
                // Si falla el logo, generar sin él
                generarContenido(doc, titulo, fecha, hora, columnas, datos);
            };
        } else {
            generarContenido(doc, titulo, fecha, hora, columnas, datos);
        }
    }
};

function generarContenido(doc, titulo, fecha, hora, columnas, datos) {
    // Título y Encabezado
    doc.setFontSize(18);
    doc.setTextColor(132, 42, 59); // Color Guindo (#842A3B)
    doc.text(titulo, 50, 20);
    
    doc.setFontSize(10);
    doc.setTextColor(100);
    doc.text(`Fecha: ${fecha} - Hora: ${hora}`, 50, 26);
    doc.text("Sistema de Control de Laboratorios - PachaSoft", 50, 31);

    // Línea separadora
    doc.setDrawColor(200);
    doc.line(14, 35, 196, 35);

    // Tabla
    doc.autoTable({
        startY: 40,
        head: [columnas],
        body: datos,
        theme: 'grid',
        headStyles: {
            fillColor: [132, 42, 59], // Guindo
            textColor: 255,
            fontSize: 8,
            fontStyle: 'bold',
            halign: 'center'
        },
        bodyStyles: {
            fontSize: 7,
            cellPadding: 2
        },
        alternateRowStyles: {
            fillColor: [245, 245, 245]
        },
        margin: { top: 40 },
        didDrawPage: function (data) {
            // Pie de página
            doc.setFontSize(8);
            doc.setTextColor(150);
            doc.text(
                'Página ' + doc.internal.getNumberOfPages(),
                data.settings.margin.left,
                doc.internal.pageSize.height - 10
            );
        }
    });

    // Guardar PDF
    const nombreArchivo = `${titulo.replace(/\s+/g, '_')}_${new Date().getTime()}.pdf`;
    doc.save(nombreArchivo);
}
