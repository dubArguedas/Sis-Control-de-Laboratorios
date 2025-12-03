# 🖥️ Sistema de Registro, Gestión y Control de Laboratorios de Cómputo – UPDS  
## Equipo de Desarrollo: **PachaSPTF**  

---

## 📌 Descripción del Proyecto

Este sistema tiene como objetivo digitalizar y automatizar todo el proceso de control de los laboratorios de cómputo de la **Universidad Privada del Valle (UNIVALLE)**.  
Actualmente, el registro de asistencia y observaciones se realiza mediante hojas físicas, lo cual genera:

- Pérdida de información  
- Datos duplicados o ilegibles  
- Falta de trazabilidad
- Imposibilidad de reportes en tiempo real  
- Incapacidad de monitorear el estado de las máquinas  

El sistema desarrollado por **PachaSPTF** soluciona estas limitaciones mediante:

- Registro de asistencia con **códigos QR**
- Validación digital del docente
- Control del estado de computadores en tiempo real
- Gestión de uso libre de laboratorios
- Reportes automáticos (PDF)
- Paneles para encargado, docente y estudiantes

Este proyecto fue desarrollado bajo buenas prácticas XP y metodología ágil.

## 🚀 Tecnologías Utilizadas

### **Frontend**
- Blazor WebAssembly
- HTML / CSS / Bootstrap / MudBlazor

### **Backend**
- ASP.NET Core 8 Web API
- Entity Framework Core
- JWT Authentication

### **Base de Datos**
- MySQL

### **Otras Herramientas**
- Git / GitHub
- Render / Azure / Railway (para despliegue)
- Librerías para QR (QRCoder)

---

## 🧩 Arquitectura del Sistema
- Se desarrollo con una arquitectura en CAPAS


---

## 📚 Funcionalidades Principales

### ✔ **Estudiantes**
- Registro automático por QR  
- Observaciones en máquinas  
- Historial de asistencias  

### ✔ **Docentes**
- Validación de clases  
- Lista de estudiantes conectados en tiempo real  
- Reportes por materia  

### ✔ **Encargado**
- Ver estado de todas las máquinas  
- Registrar fallas  
- Gestionar alertas  
- Revisar observaciones  
- Reportes diarios, semanales y mensuales  

### ✔ **Administración**
- Gestión de laboratorios  
- Mantenimiento preventivo  
- Control de acceso  

---

# 👥 Equipo de Desarrollo – *PachaSoft*  
### *Estudiantes de 4to semestre – Ingeniería de Sistemas, Universidad Privada del Valle*

| Rol | Nombre | Foto | Responsabilidades |
|-----|--------|-------|-------------------|
| Líder XP / Backend | **Jhael Arguedas** | <img src="https://github.com/dubArguedas.png" width="70"> | Coordinar equipo, facilitar prácticas XP, backend, API, BD. |
| Backend | **Carlos Conde** | <img src="https://github.com/Carlos-Eduardo-Conde-M.png" width="70"> | Lógica del servidor, endpoints, pruebas unitarias. |
| Frontend | **Angel Paredes** | <img src="https://github.com/AngelParedesH20.png" width="70"> | UI/UX, páginas, componentes, API. |
| Frontend | **John Zabaleta** | <img src="https://github.com/Riceious.png" width="70"> | Interfaz, dashboards, usuario. |
| QA | **Equipo PachaSPTF** | — | Validación, pruebas y control de calidad. |


---

## 📦 Instalación del Proyecto

### 1️⃣ Clonar repositorio
```bash
git clone https://github.com/tu-usuario/NombreDelProyecto.git


