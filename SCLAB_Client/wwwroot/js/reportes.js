window.reportes = {
    generarPDF: function (titulo, datos, columnas, logoUrl) {
        return new Promise((resolve, reject) => {
            try {
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF('portrait');

                const ahora = new Date();
                const fecha = ahora.toLocaleDateString('es-ES', { day: '2-digit', month: 'long', year: 'numeric' });
                const diaSemana = ahora.toLocaleDateString('es-ES', { weekday: 'long' });
                const hora = ahora.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', hour12: false });

                const finalizarPDF = (img) => {
                    generarContenido(doc, titulo, fecha, hora, diaSemana, columnas, datos, img);
                    const blobUrl = doc.output('bloburl');
                    resolve(blobUrl);
                };

                if (logoUrl) {
                    const img = new Image();
                    img.src = logoUrl;
                    img.onload = () => finalizarPDF(img);
                    img.onerror = () => finalizarPDF(null);
                } else {
                    finalizarPDF(null);
                }
            } catch (error) {
                reject(error);
            }
        });
    }
};

function generarContenido(doc, titulo, fecha, hora, diaSemana, columnas, datos, logoImg) {
    const colorPrincipal = [132, 42, 59];
    const colorAccent = [191, 75, 87];
    const colorDark = [26, 26, 26];
    const colorGray = [120, 120, 120];
    const colorSuccess = [34, 139, 34];
    const colorDanger = [220, 53, 69];
    const colorInfo = [52, 152, 219];

    // ═══════════════════════════════════════════════════════════
    // 1. HEADER
    // ═══════════════════════════════════════════════════════════
    doc.setFillColor(...colorPrincipal);
    doc.rect(0, 0, 210, 42, 'F');
    doc.setFillColor(...colorAccent);
    doc.rect(0, 0, 210, 2, 'F');

    doc.setDrawColor(255, 255, 255);
    doc.setLineWidth(0.1);
    for (let i = 0; i < 12; i++) {
        doc.line(160 + (i * 6), 0, 190 + (i * 6), 42);
    }

    if (logoImg) {
        doc.setFillColor(255, 255, 255);
        doc.circle(28, 21, 14, 'F');
        doc.setDrawColor(...colorAccent);
        doc.setLineWidth(1.8);
        doc.circle(28, 21, 14, 'S');
        doc.setDrawColor(255, 255, 255);
        doc.setLineWidth(0.4);
        doc.circle(28, 21, 15.5, 'S');
        doc.addImage(logoImg, 'PNG', 15, 11, 26, 20);
    } else {
        doc.setFillColor(255, 255, 255);
        doc.circle(28, 21, 14, 'F');
        doc.setDrawColor(...colorAccent);
        doc.setLineWidth(1.8);
        doc.circle(28, 21, 14, 'S');
        doc.setFontSize(16);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...colorPrincipal);
        doc.text('SCLAB', 28, 23, { align: 'center' });
    }

    // Título un poco más pequeño (18 en vez de 20)
    doc.setFontSize(18);
    doc.setFont('helvetica', 'bold');
    doc.setTextColor(255, 255, 255);
    doc.text(titulo.toUpperCase(), 48, 15);

    doc.setDrawColor(255, 255, 255);
    doc.setLineWidth(0.7);
    doc.line(48, 18, 195, 18);

    // Subtítulo (8 en vez de 9)
    doc.setFontSize(8);
    doc.setFont('helvetica', 'normal');
    doc.setTextColor(255, 255, 255);
    doc.text('Sistema de Control de Laboratorios • PachaSoft', 48, 23);

    const diaCapitalizado = diaSemana.charAt(0).toUpperCase() + diaSemana.slice(1);

    // Chips con letra más pequeña (6.5 en vez de 7.5)
    doc.setFontSize(6.5);
    doc.setFont('helvetica', 'bold');

    doc.setFillColor(255, 255, 255);
    doc.roundedRect(48, 27, 65, 6, 1.5, 1.5, 'F');
    doc.setTextColor(...colorPrincipal);
    doc.text(diaCapitalizado + ', ' + fecha, 52, 30.5);

    doc.setFillColor(255, 255, 255);
    doc.roundedRect(116, 27, 26, 6, 1.5, 1.5, 'F');
    doc.setTextColor(...colorPrincipal);
    doc.text(hora, 119, 30.5);

    doc.setFillColor(...colorAccent);
    doc.roundedRect(145, 27, 40, 6, 1.5, 1.5, 'F');
    doc.setTextColor(255, 255, 255);
    doc.text(datos.length + ' registros', 148, 30.5);

    // ═══════════════════════════════════════════════════════════
    // 2. PROCESAMIENTO
    // ═══════════════════════════════════════════════════════════
    const datosFormateados = datos.map(fila => {
        return fila.map((celda, index) => {
            const nombreColumna = columnas[index].toLowerCase();

            if ((nombreColumna.includes('ingreso') || nombreColumna.includes('salida'))) {
                if (!celda || celda === '-') return '-';
                const match = String(celda).match(/(\d{1,2}:\d{2})/);
                if (match) {
                    const partes = match[0].split(':');
                    return partes[0].padStart(2, '0') + ':' + partes[1];
                }
                return '-';
            }

            if (nombreColumna.includes('fecha') && celda) {
                try {
                    const f = new Date(celda);
                    if (!isNaN(f)) return f.toLocaleDateString('es-ES', { day: '2-digit', month: '2-digit', year: '2-digit' });
                } catch (e) { }
            }
            return celda || '-';
        });
    });

    // ═══════════════════════════════════════════════════════════
    // 3. TABLA (Fuentes reducidas en 1 punto)
    // ═══════════════════════════════════════════════════════════
    doc.autoTable({
        startY: 45,
        head: [columnas],
        body: datosFormateados,
        theme: 'plain',

        headStyles: {
            fillColor: colorPrincipal,
            textColor: [255, 255, 255],
            fontSize: 6.5, // ANTES 7.5 -> AHORA 6.5
            fontStyle: 'bold',
            halign: 'center',
            valign: 'middle',
            cellPadding: { top: 3, right: 2, bottom: 3, left: 2 },
            lineWidth: 0,
            minCellHeight: 7 // Reducido para ser más compacto
        },
        bodyStyles: {
            fontSize: 6, // ANTES 7 -> AHORA 6
            cellPadding: { top: 2, right: 2, bottom: 2, left: 2 }, // Padding un poco más ajustado
            textColor: colorDark,
            lineWidth: 0,
            valign: 'middle',
            halign: 'center'
        },
        alternateRowStyles: {
            fillColor: [250, 250, 252]
        },

        columnStyles: {
            0: { halign: 'center', cellWidth: 16 },                    // Fecha
            1: { halign: 'left', fontStyle: 'bold', textColor: colorPrincipal, cellWidth: 28, fontSize: 6 }, // Usuario
            2: { halign: 'center', cellWidth: 14 },                    // Rol
            3: { halign: 'center', fontStyle: 'bold', textColor: [46, 125, 50], cellWidth: 18, fontSize: 5.5 }, // Tipo
            4: { halign: 'center', fontStyle: 'bold', textColor: [233, 30, 99], cellWidth: 14, fontSize: 5.5 }, // Registro
            5: { halign: 'center', fontStyle: 'bold', textColor: colorAccent, cellWidth: 10, fontSize: 6 },    // Lab
            6: { halign: 'left', cellWidth: 20 },                      // Materia
            7: { halign: 'left', fontSize: 5, textColor: colorGray, cellWidth: 'auto' },  // Observación
            8: { halign: 'center', fontStyle: 'bold', textColor: colorSuccess, cellWidth: 13, fontSize: 6 }, // Ingreso
            9: { halign: 'center', fontStyle: 'bold', textColor: colorDanger, cellWidth: 13, fontSize: 6 }   // Salida
        },

        margin: { top: 45, left: 10, right: 10, bottom: 25 },

        didDrawCell: function (data) {
            if (data.section === 'body' && data.row.index > 0) {
                doc.setDrawColor(240, 240, 240);
                doc.setLineWidth(0.1);
                doc.line(data.cell.x, data.cell.y, data.cell.x + data.cell.width, data.cell.y);
            }
        },

        didDrawPage: function (data) {
            const pageHeight = doc.internal.pageSize.height;
            const pageWidth = doc.internal.pageSize.width;

            doc.setFillColor(...colorPrincipal);
            doc.rect(0, pageHeight - 20, 210, 20, 'F');
            doc.setFillColor(...colorAccent);
            doc.rect(0, pageHeight - 20, 210, 1.5, 'F');
            doc.setDrawColor(255, 255, 255);
            doc.setLineWidth(0.1);
            for (let i = 0; i < 8; i++) {
                doc.line(15 + (i * 6), pageHeight - 20, 25 + (i * 6), pageHeight);
            }

            // Footer fuente reducida (7.5 en vez de 8.5)
            doc.setFontSize(7.5);
            doc.setFont('helvetica', 'bold');
            doc.setTextColor(255, 255, 255);
            doc.text('Página ' + data.pageNumber + ' / ' + doc.internal.getNumberOfPages(), pageWidth / 2, pageHeight - 11, { align: 'center' });

            doc.setFontSize(6);
            doc.setFont('helvetica', 'normal');
            doc.text('© 2024 PachaSoft Solutions • Sistema de Control de Laboratorios', pageWidth / 2, pageHeight - 6, { align: 'center' });

            doc.setFontSize(5);
            doc.text('CONFIDENCIAL', 15, pageHeight - 10);
            doc.text('Generado: ' + fecha + ' ' + hora, pageWidth - 15, pageHeight - 10, { align: 'right' });
        }
    });

    // ═══════════════════════════════════════════════════════════
    // 4. TARJETAS (Fuentes reducidas)
    // ═══════════════════════════════════════════════════════════
    let finalY = doc.lastAutoTable.finalY + 8;
    const pageHeight = doc.internal.pageSize.height;

    if (finalY > pageHeight - 45) {
        doc.addPage();
        finalY = 20;
    }

    const cardWidth = 60;
    const cardGap = 5;
    let currentX = 10;

    const drawFooterCard = (x, y, titulo, linea1, linea2, colorBorde) => {
        doc.setFillColor(255, 255, 255);
        doc.roundedRect(x, y, cardWidth, 14, 1, 1, 'F');
        doc.setDrawColor(...colorBorde);
        doc.setLineWidth(0.5);
        doc.roundedRect(x, y, cardWidth, 14, 1, 1, 'S');
        doc.setFillColor(...colorBorde);
        doc.rect(x, y, 2, 14, 'F');

        // Título de tarjeta (6 en vez de 6.5)
        doc.setFontSize(6);
        doc.setFont('helvetica', 'bold');
        doc.setTextColor(...colorBorde);
        doc.text(titulo, x + 5, y + 4.5);

        // Texto de tarjeta (5 en vez de 5.5)
        doc.setFontSize(5);
        doc.setFont('helvetica', 'normal');
        doc.setTextColor(...colorGray);
        doc.text(linea1, x + 5, y + 8);
        doc.text(linea2, x + 5, y + 11.5);
    };

    drawFooterCard(currentX, finalY, 'DOCUMENTO', 'Reporte Asistencias v2.0', 'Formato PDF Profesional', colorPrincipal);
    currentX += cardWidth + cardGap;
    drawFooterCard(currentX, finalY, 'PERÍODO', `Fecha: ${fecha}`, `Hora: ${hora} hrs`, colorInfo);
    currentX += cardWidth + cardGap;
    drawFooterCard(currentX, finalY, 'ESTADÍSTICAS', `Registros: ${datos.length}`, 'Estado: Completado', colorSuccess);
}