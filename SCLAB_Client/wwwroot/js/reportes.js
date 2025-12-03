window.reportes = {
    generarPDF: function (titulo, datos, columnas, logoUrl, chartData = null, chartType = null, stats = null, filtros = null) {
        console.log("Generando PDF con filtros:", filtros);
        return new Promise((resolve, reject) => {
            try {
                if (!window.jspdf) throw new Error("jsPDF no cargado.");
                const { jsPDF } = window.jspdf;
                const doc = new jsPDF('portrait');

                if (typeof doc.autoTable !== 'function') throw new Error("AutoTable no cargado.");

                const ahora = new Date();
                const fecha = ahora.toLocaleDateString('es-ES', { day: '2-digit', month: 'long', year: 'numeric' });
                const diaSemana = ahora.toLocaleDateString('es-ES', { weekday: 'long' });
                const hora = ahora.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit', hour12: false });

                const nombreArchivoFinal = generarNombreArchivo(titulo, filtros);
                const img = new Image();
                let isImageProcessed = false;

                const finalizarPDF = (loadedImg) => {
                    if (isImageProcessed) return;
                    isImageProcessed = true;
                    try {
                        generarContenido(doc, titulo, fecha, hora, diaSemana, columnas, datos, loadedImg, chartData, chartType, stats, filtros);
                        const blobUrl = doc.output('bloburl');
                        resolve({ blobUrl: blobUrl, nombrePDF: nombreArchivoFinal });
                    } catch (err) { reject(err.message); }
                };

                // Timeout de seguridad para la imagen
                setTimeout(() => finalizarPDF(null), 2000);

                if (logoUrl && logoUrl.trim() !== "") {
                    img.src = logoUrl;
                    img.onload = () => finalizarPDF(img);
                    img.onerror = () => finalizarPDF(null);
                } else {
                    finalizarPDF(null);
                }
            } catch (error) { reject(error.message); }
        });
    }
};

function generarNombreArchivo(tituloBase, filtros) {
    let nombre = "Reporte";
    if (!filtros) return `${nombre}_${new Date().getTime()}.pdf`;

    const partes = [];

    // 1. Contexto (Día o Tipo)
    if (filtros.dia_semana) partes.push(filtros.dia_semana);
    else partes.push("General");

    // 2. Rol
    if (filtros.rol && filtros.rol !== 'todos') partes.push(filtros.rol.toUpperCase());

    // 3. Materia (Limpiar caracteres especiales)
    if (filtros.materia) partes.push(filtros.materia.replace(/[^a-zA-Z0-9]/g, "").substring(0, 15));

    // 4. UBICACIÓN (Torre) - Prioridad alta
    if (filtros.ubicacion) partes.push(filtros.ubicacion.replace(/[^a-zA-Z0-9]/g, ""));

    // 5. LABORATORIO
    if (filtros.laboratorio && filtros.laboratorio !== '0') partes.push(filtros.laboratorio.replace(/[^a-zA-Z0-9]/g, ""));

    // 6. MÁQUINA
    if (filtros.maquina) partes.push(filtros.maquina.replace(/[^a-zA-Z0-9]/g, ""));

    // 7. Tipo Uso
    if (filtros.tipo_uso && filtros.tipo_uso !== 'general') {
        partes.push(filtros.tipo_uso === 'uso_libre' ? 'LIBRE' : 'PROG');
    }

    // 8. Fecha
    const fechaStr = filtros.fecha_inicio ? filtros.fecha_inicio.replaceAll("-", "") : new Date().toISOString().slice(0, 10).replaceAll("-", "");
    partes.push(fechaStr);

    return nombre + "_" + partes.join("_") + ".pdf";
}

function generarContenido(doc, titulo, fecha, hora, diaSemana, columnas, datos, logoImg, chartData, chartType, stats, filtros) {
    const colorPrincipal = [132, 42, 59];
    const colorAccent = [191, 75, 87];

    // Header
    doc.setFillColor(...colorPrincipal);
    doc.rect(0, 0, 210, 42, 'F');
    doc.setFillColor(...colorAccent);
    doc.rect(0, 40, 210, 2, 'F');

    if (logoImg) {
        doc.setFillColor(255);
        doc.circle(28, 21, 14, 'F');
        doc.addImage(logoImg, 'PNG', 15, 11, 26, 20);
    }

    doc.setFont('helvetica', 'bold');
    doc.setTextColor(255);
    doc.setFontSize(14);

    // Manejo de título largo
    let tituloMostrar = titulo.toUpperCase().replace(" - ", "\n");
    const titleLines = doc.splitTextToSize(tituloMostrar, 140);
    doc.text(titleLines, 48, (titleLines.length > 1 ? 12 : 15));

    doc.setFontSize(8);
    doc.setFont('helvetica', 'normal');
    doc.text('Sistema de Control de Laboratorios • Reporte Oficial', 48, 32);

    doc.setFontSize(7);
    doc.setFont('helvetica', 'bold');
    doc.text(`${diaSemana.toUpperCase()}, ${fecha} - ${hora}`, 195, 32, { align: 'right' });

    // Filtros aplicados (texto pequeño sobre la tabla)
    let startY = 50;
    if (filtros) {
        doc.setTextColor(100);
        doc.setFontSize(7);
        const fParts = [];
        if (filtros.ubicacion) fParts.push(`Ubi: ${filtros.ubicacion}`);
        if (filtros.laboratorio) fParts.push(`Lab: ${filtros.laboratorio}`);
        if (filtros.maquina) fParts.push(`Maq: ${filtros.maquina}`);
        if (filtros.hora_inicio) fParts.push(`Horario: ${filtros.hora_inicio} - ${filtros.hora_fin || ''}`);

        if (fParts.length > 0) {
            doc.text("Filtros: " + fParts.join(" | "), 10, 48);
            startY = 52;
        }
    }

    // Tabla
    doc.autoTable({
        startY: startY,
        head: [columnas],
        body: datos,
        theme: 'grid',
        styles: { fontSize: 7, cellPadding: 2, halign: 'center', lineWidth: 0.1 },
        headStyles: { fillColor: colorPrincipal, textColor: 255, fontStyle: 'bold' },
        alternateRowStyles: { fillColor: [250, 250, 252] },
        columnStyles: {
            0: { cellWidth: 18 }, // Fecha
            1: { cellWidth: 35, halign: 'left' }, // Usuario
            5: { cellWidth: 25, halign: 'left' }, // Materia
            8: { cellWidth: 'auto', halign: 'left' } // Observacion
        },
        margin: { top: 50, bottom: 25 },
        didDrawPage: function (data) {
            doc.setFontSize(7);
            doc.setTextColor(100);
            doc.text(`Página ${data.pageNumber}`, 105, doc.internal.pageSize.height - 10, { align: 'center' });
        }
    });

    let finalY = doc.lastAutoTable.finalY + 10;

    // Estadísticas
    if (finalY > doc.internal.pageSize.height - 70) { doc.addPage(); finalY = 20; }

    if (stats || chartData) {
        doc.setFillColor(...colorPrincipal);
        doc.rect(10, finalY, 190, 8, 'F');
        doc.setTextColor(255);
        doc.setFontSize(9);
        doc.setFont('helvetica', 'bold');
        doc.text("RESUMEN ESTADÍSTICO Y TENDENCIAS", 105, finalY + 5.5, { align: 'center' });
        finalY += 15;

        if (stats) {
            dibujarTarjetasStats(doc, finalY, stats);
            finalY += 25;
        }

        if (chartData && chartData.length > 0) {
            doc.setTextColor(20);
            doc.setFontSize(9);
            // Título del gráfico cambiado para reflejar que es una tendencia horaria
            doc.text("Tendencia de Asistencia (Por Hora):", 10, finalY);
            finalY += 5;
            dibujarGraficoBarras(doc, 10, finalY, 190, 60, chartData, colorAccent);
        }
    }
}

function dibujarTarjetasStats(doc, y, stats) {
    const cardWidth = 45;
    const items = [
        { lbl: "TOTAL REGISTROS", val: stats.totalRows },
        { lbl: "USUARIOS REGISTRADOS", val: stats.uniqueUsers },
        { lbl: "MÁQUINAS USADAS", val: stats.uniqueMachines }
    ];
    let x = 37;
    items.forEach(item => {
        doc.setDrawColor(200); doc.setFillColor(250); doc.roundedRect(x, y, cardWidth, 15, 2, 2, 'FD');
        doc.setFontSize(7); doc.setTextColor(100); doc.text(item.lbl, x + (cardWidth / 2), y + 5, { align: 'center' });
        doc.setFontSize(10); doc.setTextColor(0); doc.text(item.val.toString(), x + (cardWidth / 2), y + 11, { align: 'center' });
        x += cardWidth + 5;
    });
}

//function dibujarGraficoBarras(doc, x, y, w, h, data, colorBarra) {
//    if (!data || data.length === 0) return;

//    // MODIFICADO: No cortamos con slice(0,7) para permitir ver todas las horas (7:00 a 20:00)
//    // Pero si son demasiados datos (>15), limitamos para que no se rompa el dibujo
//    const chartData = data.length > 15 ? data.slice(0, 15) : data;

//    let maxVal = Math.max(...chartData.map(d => Number(d.Value) || 0));
//    if (maxVal <= 0) maxVal = 1;

//    doc.setDrawColor(150); doc.setLineWidth(0.3);
//    doc.line(x + 10, y + h, x + w, y + h); // Eje X
//    doc.line(x + 10, y, x + 10, y + h);    // Eje Y

//    const barWidth = (w - 20) / chartData.length;
//    const maxBarHeight = h - 10;

//    chartData.forEach((item, i) => {
//        const val = Number(item.Value) || 0;
//        const barHeight = (val / maxVal) * maxBarHeight;
//        const currentX = x + 15 + (i * barWidth);
//        const currentY = y + h - barHeight;

//        doc.setFillColor(...colorBarra);
//        doc.rect(currentX, currentY, barWidth - 3, barHeight, 'F'); // Barras más delgadas para que entren más horas

//        // Valor encima
//        doc.setTextColor(0); doc.setFontSize(7);
//        if (val > 0) doc.text(val.toString(), currentX + (barWidth - 3) / 2, currentY - 2, { align: 'center' });

//        // Etiqueta hora (Categoría)
//        doc.setFontSize(6);
//        let label = item.Category ? item.Category.toString() : "";
//        doc.text(label, currentX + (barWidth - 3) / 2, y + h + 4, { align: 'center' });
//    });
//}