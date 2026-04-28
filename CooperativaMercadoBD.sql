CREATE DATABASE CooperativaBD;
GO
USE CooperativaBD;
GO

CREATE TABLE Socio (
    IdSocio INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(100) NOT NULL,
    DNI NVARCHAR(20) NULL,
    Telefono NVARCHAR(20) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    CONSTRAINT UQ_Socio_DNI UNIQUE (DNI)
)
GO

CREATE TABLE Usuario (
    IdUsuario INT IDENTITY PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Password NVARCHAR(100) NOT NULL,
    Rol NVARCHAR(20) NOT NULL CHECK (Rol IN ('Admin','Socio')),
    IdSocio INT NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (IdSocio) REFERENCES Socio(IdSocio)
)
GO

CREATE TABLE Puesto (
    IdPuesto INT IDENTITY PRIMARY KEY,
    Numero NVARCHAR(10) NOT NULL UNIQUE,
    Metraje DECIMAL(5,2) NULL CHECK (Metraje >= 0),
    Ubicacion NVARCHAR(100) NULL,
    Giro NVARCHAR(50) NULL,
    MontoAlquiler DECIMAL(10,2) NOT NULL CHECK (MontoAlquiler >= 0),
    IdSocio INT NULL,
    Observaciones NVARCHAR(500) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (IdSocio) REFERENCES Socio(IdSocio)
)
GO

CREATE TABLE HistorialPuesto (
    IdHistorial INT IDENTITY PRIMARY KEY,
    IdPuesto INT NOT NULL,
    IdSocio INT NOT NULL,
    FechaInicio DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    FechaFin DATE NULL,
    MotivoRetiro NVARCHAR(200) NULL,
    Activo BIT NOT NULL DEFAULT 1,
    FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto),
    FOREIGN KEY (IdSocio) REFERENCES Socio(IdSocio)
)
GO

CREATE INDEX IX_HistorialPuesto_IdPuesto ON HistorialPuesto(IdPuesto);
CREATE INDEX IX_HistorialPuesto_IdSocio ON HistorialPuesto(IdSocio);
CREATE INDEX IX_HistorialPuesto_Activo ON HistorialPuesto(Activo);
GO

CREATE TABLE TipoDeuda (
    IdTipoDeuda INT IDENTITY PRIMARY KEY,
    Nombre NVARCHAR(50) NOT NULL UNIQUE,
    MontoBase DECIMAL(10,2) NULL CHECK (MontoBase >= 0),
    Activo BIT NOT NULL DEFAULT 1
)
GO

CREATE TABLE Deuda (
    IdDeuda INT IDENTITY PRIMARY KEY,
    IdPuesto INT NOT NULL,
    IdTipoDeuda INT NOT NULL,
    Descripcion NVARCHAR(100) NULL,
    Monto DECIMAL(10,2) NOT NULL CHECK (Monto > 0),
    Mora DECIMAL(10,2) DEFAULT 0 CHECK (Mora >= 0),
    Mes INT NOT NULL CHECK (Mes BETWEEN 1 AND 12),
    Anio INT NOT NULL CHECK (Anio >= 2020),
    FechaVencimiento DATE NULL,
    FechaAplicacionMora DATETIME NULL,
    UsuarioAplicaMora INT NULL,
    Estado NVARCHAR(20) NOT NULL DEFAULT 'Pendiente' CHECK (Estado IN ('Pendiente','Pagado')),
    FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto),
    FOREIGN KEY (IdTipoDeuda) REFERENCES TipoDeuda(IdTipoDeuda),
    FOREIGN KEY (UsuarioAplicaMora) REFERENCES Usuario(IdUsuario),
    CONSTRAINT UQ_Deuda UNIQUE (IdPuesto, IdTipoDeuda, Mes, Anio)
)
GO

CREATE TABLE Pago (
    IdPago INT IDENTITY PRIMARY KEY,
    IdDeuda INT NOT NULL,
    Monto DECIMAL(10,2) NOT NULL CHECK (Monto > 0),
    Fecha DATETIME NOT NULL DEFAULT GETDATE(),
    NumeroRecibo NVARCHAR(30) NOT NULL UNIQUE,
    MetodoPago NVARCHAR(20) NULL,
    FOREIGN KEY (IdDeuda) REFERENCES Deuda(IdDeuda)
)
GO

CREATE TABLE IngresoDiario (
    IdIngreso INT IDENTITY PRIMARY KEY,
    IdPuesto INT NOT NULL,
    Fecha DATE NOT NULL,
    Monto DECIMAL(10,2) NOT NULL CHECK (Monto > 0),
    FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
    IdUsuario INT NULL,
    FOREIGN KEY (IdPuesto) REFERENCES Puesto(IdPuesto),
    FOREIGN KEY (IdUsuario) REFERENCES Usuario(IdUsuario),
    CONSTRAINT UQ_Ingreso UNIQUE (IdPuesto, Fecha)
);
GO

-- STORED PROCEDURES - SOCIO

CREATE OR ALTER PROCEDURE sp_RegistrarSocio
    @Nombre NVARCHAR(100),
    @DNI NVARCHAR(20) = NULL,
    @Telefono NVARCHAR(20) = NULL,
    @IdSocioOut INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Nombre IS NULL OR LTRIM(RTRIM(@Nombre)) = ''
    BEGIN
        RAISERROR('El nombre es obligatorio.', 16, 1);
        RETURN;
    END

    IF @DNI IS NOT NULL AND EXISTS (SELECT 1 FROM Socio WHERE DNI = @DNI)
    BEGIN
        RAISERROR('Ya existe un socio con ese DNI.', 16, 1);
        RETURN;
    END

    INSERT INTO Socio (Nombre, DNI, Telefono, Activo)
    VALUES (@Nombre, @DNI, @Telefono, 1);

    SET @IdSocioOut = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_ActualizarSocio
    @IdSocio INT,
    @Nombre NVARCHAR(100),
    @DNI NVARCHAR(20) = NULL,
    @Telefono NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Socio WHERE IdSocio = @IdSocio)
    BEGIN
        RAISERROR('El socio no existe.', 16, 1);
        RETURN;
    END

    IF @Nombre IS NULL OR LTRIM(RTRIM(@Nombre)) = ''
    BEGIN
        RAISERROR('El nombre es obligatorio.', 16, 1);
        RETURN;
    END

    IF @DNI IS NOT NULL AND EXISTS (
        SELECT 1 FROM Socio
        WHERE DNI = @DNI AND IdSocio <> @IdSocio
    )
    BEGIN
        RAISERROR('Ya existe otro socio con ese DNI.', 16, 1);
        RETURN;
    END

    UPDATE Socio
    SET Nombre = @Nombre,
        DNI = @DNI,
        Telefono = @Telefono
    WHERE IdSocio = @IdSocio;
END
GO

CREATE OR ALTER PROCEDURE sp_ListarSocios
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Socio ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerSocioPorId
    @IdSocio INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM Socio WHERE IdSocio = @IdSocio;
END
GO

CREATE OR ALTER PROCEDURE sp_ValidarRetiroSocio
    @IdSocio INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        COUNT(*) AS DeudasPendientes
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    WHERE p.IdSocio = @IdSocio AND d.Estado = 'Pendiente';
END
GO

CREATE OR ALTER PROCEDURE sp_RetirarSocio
    @IdSocio INT,
    @MotivoRetiro NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
    
        IF NOT EXISTS (SELECT 1 FROM Socio WHERE IdSocio = @IdSocio)
        BEGIN
            RAISERROR('El socio no existe.', 16, 1);
            RETURN;
        END

        DECLARE @DeudasPendientes INT;

        SELECT @DeudasPendientes = COUNT(*)
        FROM Deuda d
        INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
        WHERE p.IdSocio = @IdSocio AND d.Estado = 'Pendiente';

        IF @DeudasPendientes > 0
        BEGIN
            DECLARE @MensajeError NVARCHAR(200);
            SET @MensajeError = 'El socio tiene ' + CAST(@DeudasPendientes AS NVARCHAR) + ' deudas pendientes. No puede retirarse.';
            RAISERROR(@MensajeError, 16, 1);
            RETURN;
        END

        DECLARE @MotivoHistorial NVARCHAR(200);
        SET @MotivoHistorial = 'Retiro del socio: ' + @MotivoRetiro;

        UPDATE HistorialPuesto
        SET FechaFin = CAST(GETDATE() AS DATE),
            MotivoRetiro = @MotivoHistorial,
            Activo = 0
        WHERE IdSocio = @IdSocio AND FechaFin IS NULL;

        UPDATE Puesto
        SET IdSocio = NULL
        WHERE IdSocio = @IdSocio;

        UPDATE Socio
        SET Activo = 0
        WHERE IdSocio = @IdSocio;

        UPDATE Usuario
        SET Activo = 0
        WHERE IdSocio = @IdSocio;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO
CREATE OR ALTER PROCEDURE sp_ReactivarSocio
    @IdSocio INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Socio WHERE IdSocio = @IdSocio)
    BEGIN
        RAISERROR('El socio no existe.', 16, 1);
        RETURN;
    END

    -- Reactivar socio
    UPDATE Socio
    SET Activo = 1
    WHERE IdSocio = @IdSocio;

    -- Reactivar usuario vinculado
    UPDATE Usuario
    SET Activo = 1
    WHERE IdSocio = @IdSocio;
END
GO

-- STORED PROCEDURES - USUARIO


CREATE OR ALTER PROCEDURE sp_ValidarUsuario
    @Username NVARCHAR(50),
    @Password NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdUsuario, Username, Rol, IdSocio, Activo
    FROM Usuario
    WHERE Username = @Username AND [Password] = @Password AND Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerUsuarioPorUsername
    @Username NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT IdUsuario, Username, Rol, IdSocio, Activo
    FROM Usuario
    WHERE Username = @Username AND Activo = 1;
END
GO

CREATE OR ALTER PROCEDURE sp_CrearUsuarioConSocio
    @Username NVARCHAR(50),
    @Password NVARCHAR(100),
    @NombreSocio NVARCHAR(100),
    @DNI NVARCHAR(20) = NULL,
    @Telefono NVARCHAR(20) = NULL,
    @IdSocioOut INT OUTPUT,
    @IdUsuarioOut INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        IF EXISTS (SELECT 1 FROM Usuario WHERE Username = @Username)
        BEGIN
            RAISERROR('El nombre de usuario ya existe.', 16, 1);
            RETURN;
        END

        IF @DNI IS NOT NULL AND EXISTS (SELECT 1 FROM Socio WHERE DNI = @DNI)
        BEGIN
            RAISERROR('Ya existe un socio con ese DNI.', 16, 1);
            RETURN;
        END

        IF @NombreSocio IS NULL OR LTRIM(RTRIM(@NombreSocio)) = ''
        BEGIN
            RAISERROR('El nombre del socio es obligatorio.', 16, 1);
            RETURN;
        END

        INSERT INTO Socio (Nombre, DNI, Telefono, Activo)
        VALUES (@NombreSocio, @DNI, @Telefono, 1);

        SET @IdSocioOut = SCOPE_IDENTITY();

        INSERT INTO Usuario (Username, Password, Rol, IdSocio, Activo)
        VALUES (@Username, @Password, 'Socio', @IdSocioOut, 1);

        SET @IdUsuarioOut = SCOPE_IDENTITY();

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

-- STORED PROCEDURES - PUESTO



CREATE OR ALTER PROCEDURE sp_RegistrarPuesto
    @Numero NVARCHAR(10),
    @Metraje DECIMAL(5,2) = NULL,
    @Ubicacion NVARCHAR(100) = NULL,
    @Giro NVARCHAR(50) = NULL,
    @MontoAlquiler DECIMAL(10,2),
    @IdSocio INT = NULL,
    @IdPuestoOut INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @Numero IS NULL OR LTRIM(RTRIM(@Numero)) = ''
    BEGIN
        RAISERROR('El número de puesto es obligatorio.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM Puesto WHERE Numero = @Numero)
    BEGIN
        RAISERROR('Ya existe un puesto con ese número.', 16, 1);
        RETURN;
    END

    IF @MontoAlquiler < 0
    BEGIN
        RAISERROR('El monto de alquiler no puede ser negativo.', 16, 1);
        RETURN;
    END

    IF @IdSocio IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Socio WHERE IdSocio = @IdSocio AND Activo = 1)
    BEGIN
        RAISERROR('El socio asignado no existe o está inactivo.', 16, 1);
        RETURN;
    END

    INSERT INTO Puesto (Numero, Metraje, Ubicacion, Giro, MontoAlquiler, IdSocio, Activo)
    VALUES (@Numero, @Metraje, @Ubicacion, @Giro, @MontoAlquiler, @IdSocio, 1);
    
    SET @IdPuestoOut = SCOPE_IDENTITY();
END
GO

CREATE OR ALTER PROCEDURE sp_ActualizarPuesto
    @IdPuesto INT,
    @Numero NVARCHAR(10),
    @Metraje DECIMAL(5,2) = NULL,
    @Ubicacion NVARCHAR(100) = NULL,
    @Giro NVARCHAR(50) = NULL,
    @MontoAlquiler DECIMAL(10,2),
    @IdSocio INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Puesto WHERE IdPuesto = @IdPuesto)
    BEGIN
        RAISERROR('El puesto no existe.', 16, 1);
        RETURN;
    END

    IF @Numero IS NULL OR LTRIM(RTRIM(@Numero)) = ''
    BEGIN
        RAISERROR('El número de puesto es obligatorio.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM Puesto
        WHERE Numero = @Numero AND IdPuesto <> @IdPuesto
    )
    BEGIN
        RAISERROR('Ya existe otro puesto con ese número.', 16, 1);
        RETURN;
    END

    IF @MontoAlquiler < 0
    BEGIN
        RAISERROR('El monto de alquiler no puede ser negativo.', 16, 1);
        RETURN;
    END

    IF @IdSocio IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Socio WHERE IdSocio = @IdSocio AND Activo = 1)
    BEGIN
        RAISERROR('El socio asignado no existe o está inactivo.', 16, 1);
        RETURN;
    END

    UPDATE Puesto
    SET Numero = @Numero,
        Metraje = @Metraje,
        Ubicacion = @Ubicacion,
        Giro = @Giro,
        MontoAlquiler = @MontoAlquiler,
        IdSocio = @IdSocio
    WHERE IdPuesto = @IdPuesto;
END
GO

CREATE OR ALTER PROCEDURE sp_AsociarPuesto
    @IdPuesto INT,
    @IdSocio INT
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Puesto WHERE IdPuesto = @IdPuesto)
        BEGIN
            RAISERROR('El puesto no existe.', 16, 1);
            RETURN;
        END

        IF NOT EXISTS (SELECT 1 FROM Socio WHERE IdSocio = @IdSocio AND Activo = 1)
        BEGIN
            RAISERROR('El socio no existe o está inactivo.', 16, 1);
            RETURN;
        END

        UPDATE HistorialPuesto
        SET FechaFin = CAST(GETDATE() AS DATE),
            Activo = 0
        WHERE IdPuesto = @IdPuesto AND FechaFin IS NULL;

        INSERT INTO HistorialPuesto (IdPuesto, IdSocio, FechaInicio)
        VALUES (@IdPuesto, @IdSocio, CAST(GETDATE() AS DATE));

        UPDATE Puesto
        SET IdSocio = @IdSocio
        WHERE IdPuesto = @IdPuesto;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE sp_DesasociarPuesto
    @IdPuesto INT,
    @Motivo NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        IF NOT EXISTS (SELECT 1 FROM Puesto WHERE IdPuesto = @IdPuesto)
        BEGIN
            RAISERROR('El puesto no existe.', 16, 1);
            RETURN;
        END

        UPDATE HistorialPuesto
        SET FechaFin = CAST(GETDATE() AS DATE),
            MotivoRetiro = @Motivo,
            Activo = 0
        WHERE IdPuesto = @IdPuesto AND FechaFin IS NULL;

        UPDATE Puesto
        SET IdSocio = NULL
        WHERE IdPuesto = @IdPuesto;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        ROLLBACK TRANSACTION;
        THROW;
    END CATCH
END
GO

CREATE OR ALTER PROCEDURE sp_ListarPuestos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.IdPuesto, p.Numero, p.Metraje, p.Ubicacion, p.Giro, 
        p.MontoAlquiler, p.IdSocio, s.Nombre AS NombreSocio, p.Activo 
    FROM Puesto p
    LEFT JOIN Socio s ON p.IdSocio = s.IdSocio
    ORDER BY p.Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerPuestoPorId
    @IdPuesto INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.IdPuesto, p.Numero, p.Metraje, p.Ubicacion, p.Giro, 
        p.MontoAlquiler, p.IdSocio, s.Nombre AS NombreSocio, p.Activo 
    FROM Puesto p
    LEFT JOIN Socio s ON p.IdSocio = s.IdSocio
    WHERE p.IdPuesto = @IdPuesto;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerPuestosPorSocio
    @IdSocio INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.IdPuesto,
        p.Numero,
        p.Metraje,
        p.Ubicacion,
        p.Giro,
        p.MontoAlquiler,
        p.Activo
    FROM Puesto p
    WHERE p.IdSocio = @IdSocio AND p.Activo = 1
    ORDER BY p.Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerHistorialPuesto
    @IdPuesto INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        h.IdHistorial,
        h.IdPuesto,
        h.IdSocio,
        s.Nombre AS NombreSocio,
        h.FechaInicio,
        h.FechaFin,
        h.MotivoRetiro,
        h.Activo
    FROM HistorialPuesto h
    INNER JOIN Socio s ON h.IdSocio = s.IdSocio
    WHERE h.IdPuesto = @IdPuesto
    ORDER BY h.FechaInicio DESC;
END
GO

-- STORED PROCEDURES - TIPO DEUDA


CREATE OR ALTER PROCEDURE sp_RegistrarTipoDeuda
    @Nombre NVARCHAR(50),
    @MontoBase DECIMAL(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Nombre IS NULL OR LTRIM(RTRIM(@Nombre)) = ''
    BEGIN
        RAISERROR('El nombre del tipo de deuda es obligatorio.', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM TipoDeuda WHERE Nombre = @Nombre)
    BEGIN
        RAISERROR('Ya existe ese tipo de deuda.', 16, 1);
        RETURN;
    END

    INSERT INTO TipoDeuda (Nombre, MontoBase, Activo)
    VALUES (@Nombre, @MontoBase, 1);
END
GO

CREATE OR ALTER PROCEDURE sp_ActualizarTipoDeuda
    @IdTipoDeuda INT,
    @Nombre NVARCHAR(50),
    @MontoBase DECIMAL(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM TipoDeuda WHERE IdTipoDeuda = @IdTipoDeuda)
    BEGIN
        RAISERROR('El tipo de deuda no existe.', 16, 1);
        RETURN;
    END

    IF @Nombre IS NULL OR LTRIM(RTRIM(@Nombre)) = ''
    BEGIN
        RAISERROR('El nombre del tipo de deuda es obligatorio.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM TipoDeuda
        WHERE Nombre = @Nombre AND IdTipoDeuda <> @IdTipoDeuda
    )
    BEGIN
        RAISERROR('Ya existe otro tipo de deuda con ese nombre.', 16, 1);
        RETURN;
    END

    UPDATE TipoDeuda
    SET Nombre = @Nombre,
        MontoBase = @MontoBase
    WHERE IdTipoDeuda = @IdTipoDeuda;
END
GO

CREATE OR ALTER PROCEDURE sp_ListarTipoDeuda
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM TipoDeuda WHERE Activo = 1 ORDER BY Nombre;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerTipoDeuda
    @IdTipoDeuda INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM TipoDeuda WHERE IdTipoDeuda = @IdTipoDeuda;
END
GO

-- STORED PROCEDURES - DEUDA

CREATE OR ALTER PROCEDURE sp_GenerarDeudasRecurrentes
    @Mes INT,
    @Anio INT
AS
BEGIN
    SET NOCOUNT ON;

    -- Variables
    DECLARE @CantidadGenerada INT = 0;
    DECLARE @FechaVencimiento DATE;

    -- Calcular fecha de vencimiento (último día del mes)
    SET @FechaVencimiento = EOMONTH(DATEFROMPARTS(@Anio, @Mes, 1));

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Generar deudas recurrentes para cada puesto ocupado
        -- Solo genera para tipos de deuda con MontoBase definido (excluyendo Alquiler)
        INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mora, Mes, Anio, FechaVencimiento, Estado)
        SELECT 
            p.IdPuesto,
            td.IdTipoDeuda,
            td.Nombre + ' mensual',
            td.MontoBase,
            0.00,
            @Mes,
            @Anio,
            @FechaVencimiento,
            'Pendiente'
        FROM Puesto p
        INNER JOIN TipoDeuda td ON td.Activo = 1 
            AND td.MontoBase IS NOT NULL
            AND td.Nombre != 'Alquiler'  -- Excluir alquiler (se genera con otro SP)
        WHERE p.Activo = 1 
            AND p.IdSocio IS NOT NULL  -- Solo puestos ocupados
            AND NOT EXISTS (
                -- Evitar duplicados: verificar que no exista ya una deuda del mismo tipo para ese puesto y mes
                SELECT 1 
                FROM Deuda d 
                WHERE d.IdPuesto = p.IdPuesto 
                    AND d.IdTipoDeuda = td.IdTipoDeuda
                    AND d.Mes = @Mes 
                    AND d.Anio = @Anio
            );

        SET @CantidadGenerada = @@ROWCOUNT;

        COMMIT TRANSACTION;

        -- Retornar resumen
        SELECT 
            @CantidadGenerada AS CantidadDeudas,
            @Mes AS Mes,
            @Anio AS Anio,
            @FechaVencimiento AS FechaVencimiento,
            'Deudas recurrentes generadas exitosamente' AS Mensaje;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE sp_GenerarDeudasParaPuesto
    @IdPuesto INT,
    @Mes INT,
    @Anio INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CantidadGenerada INT = 0;
    DECLARE @NumeroPuesto NVARCHAR(50);
    DECLARE @MontoAlquiler DECIMAL(10,2);
    DECLARE @FechaVencimiento DATE;

    -- Verificar que el puesto existe y está ocupado
    SELECT 
        @NumeroPuesto = Numero,
        @MontoAlquiler = MontoAlquiler
    FROM Puesto
    WHERE IdPuesto = @IdPuesto 
        AND Activo = 1 
        AND IdSocio IS NOT NULL;

    IF @NumeroPuesto IS NULL
    BEGIN
        RAISERROR('El puesto no existe, no está activo o no tiene socio asignado', 16, 1);
        RETURN;
    END

    SET @FechaVencimiento = EOMONTH(DATEFROMPARTS(@Anio, @Mes, 1));

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Generar ALQUILER para este puesto
        IF NOT EXISTS (
            SELECT 1 FROM Deuda 
            WHERE IdPuesto = @IdPuesto 
                AND IdTipoDeuda = (SELECT IdTipoDeuda FROM TipoDeuda WHERE Nombre = 'Alquiler')
                AND Mes = @Mes 
                AND Anio = @Anio
        )
        BEGIN
            INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mora, Mes, Anio, FechaVencimiento, Estado)
            VALUES (
                @IdPuesto,
                (SELECT IdTipoDeuda FROM TipoDeuda WHERE Nombre = 'Alquiler'),
                'Alquiler mensual',
                @MontoAlquiler,
                0.00,
                @Mes,
                @Anio,
                NULL,
                'Pendiente'
            );

            SET @CantidadGenerada = @CantidadGenerada + 1;
        END

        -- 2. Generar DEUDAS RECURRENTES para este puesto
        INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mora, Mes, Anio, FechaVencimiento, Estado)
        SELECT 
            @IdPuesto,
            td.IdTipoDeuda,
            td.Nombre + ' mensual',
            td.MontoBase,
            0.00,
            @Mes,
            @Anio,
            @FechaVencimiento,
            'Pendiente'
        FROM TipoDeuda td
        WHERE td.Activo = 1 
            AND td.MontoBase IS NOT NULL
            AND td.Nombre != 'Alquiler'
            AND NOT EXISTS (
                SELECT 1 FROM Deuda d 
                WHERE d.IdPuesto = @IdPuesto 
                    AND d.IdTipoDeuda = td.IdTipoDeuda
                    AND d.Mes = @Mes 
                    AND d.Anio = @Anio
            );

        SET @CantidadGenerada = @CantidadGenerada + @@ROWCOUNT;

        COMMIT TRANSACTION;

        -- Retornar resumen
        SELECT 
            @CantidadGenerada AS CantidadDeudas,
            @NumeroPuesto AS NumeroPuesto,
            @Mes AS Mes,
            @Anio AS Anio,
            'Deudas generadas exitosamente para el puesto ' + @NumeroPuesto AS Mensaje;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO

CREATE OR ALTER PROCEDURE sp_GenerarTodasLasDeudas
    @Mes INT,
    @Anio INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AlquileresGenerados INT = 0;
    DECLARE @RecurrentesGeneradas INT = 0;
    DECLARE @FechaVencimiento DATE;

    SET @FechaVencimiento = EOMONTH(DATEFROMPARTS(@Anio, @Mes, 1));

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Generar ALQUILERES
        INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mora, Mes, Anio, FechaVencimiento, Estado)
        SELECT 
            p.IdPuesto,
            (SELECT IdTipoDeuda FROM TipoDeuda WHERE Nombre = 'Alquiler'),
            'Alquiler mensual',
            p.MontoAlquiler,
            0.00,
            @Mes,
            @Anio,
            NULL,  -- Alquiler sin fecha de vencimiento
            'Pendiente'
        FROM Puesto p
        WHERE p.Activo = 1 
            AND p.IdSocio IS NOT NULL
            AND NOT EXISTS (
                SELECT 1 FROM Deuda d 
                WHERE d.IdPuesto = p.IdPuesto 
                    AND d.IdTipoDeuda = (SELECT IdTipoDeuda FROM TipoDeuda WHERE Nombre = 'Alquiler')
                    AND d.Mes = @Mes 
                    AND d.Anio = @Anio
            );

        SET @AlquileresGenerados = @@ROWCOUNT;

        -- 2. Generar DEUDAS RECURRENTES (Luz, Agua, Limpieza, etc.)
        INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mora, Mes, Anio, FechaVencimiento, Estado)
        SELECT 
            p.IdPuesto,
            td.IdTipoDeuda,
            td.Nombre + ' mensual',
            td.MontoBase,
            0.00,
            @Mes,
            @Anio,
            @FechaVencimiento,
            'Pendiente'
        FROM Puesto p
        INNER JOIN TipoDeuda td ON td.Activo = 1 
            AND td.MontoBase IS NOT NULL
            AND td.Nombre != 'Alquiler'
        WHERE p.Activo = 1 
            AND p.IdSocio IS NOT NULL
            AND NOT EXISTS (
                SELECT 1 FROM Deuda d 
                WHERE d.IdPuesto = p.IdPuesto 
                    AND d.IdTipoDeuda = td.IdTipoDeuda
                    AND d.Mes = @Mes 
                    AND d.Anio = @Anio
            );

        SET @RecurrentesGeneradas = @@ROWCOUNT;

        COMMIT TRANSACTION;

        -- Retornar resumen detallado
        SELECT 
            @AlquileresGenerados AS AlquileresGenerados,
            @RecurrentesGeneradas AS RecurrentesGeneradas,
            (@AlquileresGenerados + @RecurrentesGeneradas) AS TotalDeudas,
            @Mes AS Mes,
            @Anio AS Anio,
            @FechaVencimiento AS FechaVencimiento,
            'Todas las deudas generadas exitosamente' AS Mensaje;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_GenerarDeudaEspecifica
    @IdTipoDeuda INT,
    @Mes INT,
    @Anio INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CantidadGenerada INT = 0;
    DECLARE @MontoBase DECIMAL(10,2);
    DECLARE @NombreTipo NVARCHAR(100);
    DECLARE @FechaVencimiento DATE;

    -- Obtener información del tipo de deuda
    SELECT 
        @MontoBase = MontoBase,
        @NombreTipo = Nombre
    FROM TipoDeuda
    WHERE IdTipoDeuda = @IdTipoDeuda AND Activo = 1;

    -- Validar que el tipo de deuda existe y tiene monto base
    IF @MontoBase IS NULL
    BEGIN
        RAISERROR('El tipo de deuda no existe, no está activo o no tiene monto base definido', 16, 1);
        RETURN;
    END

    SET @FechaVencimiento = EOMONTH(DATEFROMPARTS(@Anio, @Mes, 1));

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Generar la deuda específica para todos los puestos ocupados
        INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mora, Mes, Anio, FechaVencimiento, Estado)
        SELECT 
            p.IdPuesto,
            @IdTipoDeuda,
            @NombreTipo + ' mensual',
            @MontoBase,
            0.00,
            @Mes,
            @Anio,
            @FechaVencimiento,
            'Pendiente'
        FROM Puesto p
        WHERE p.Activo = 1 
            AND p.IdSocio IS NOT NULL
            AND NOT EXISTS (
                SELECT 1 FROM Deuda d 
                WHERE d.IdPuesto = p.IdPuesto 
                    AND d.IdTipoDeuda = @IdTipoDeuda
                    AND d.Mes = @Mes 
                    AND d.Anio = @Anio
            );

        SET @CantidadGenerada = @@ROWCOUNT;

        COMMIT TRANSACTION;

        -- Retornar resumen
        SELECT 
            @CantidadGenerada AS CantidadGenerada,
            @NombreTipo AS TipoDeuda,
            @MontoBase AS MontoBase,
            @Mes AS Mes,
            @Anio AS Anio,
            @FechaVencimiento AS FechaVencimiento,
            'Deuda específica generada exitosamente' AS Mensaje;

    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrorMessage, 16, 1);
    END CATCH
END;
GO
CREATE OR ALTER PROCEDURE sp_CrearDeuda
    @IdPuesto INT,
    @IdTipoDeuda INT,
    @Descripcion NVARCHAR(100) = NULL,
    @Monto DECIMAL(10,2),
    @Mes INT,
    @Anio INT,
    @FechaVencimiento DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Puesto WHERE IdPuesto = @IdPuesto AND Activo = 1)
    BEGIN
        RAISERROR('El puesto no existe o está inactivo.', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM TipoDeuda WHERE IdTipoDeuda = @IdTipoDeuda AND Activo = 1)
    BEGIN
        RAISERROR('El tipo de deuda no existe o está inactivo.', 16, 1);
        RETURN;
    END

    IF @Monto <= 0
    BEGIN
        RAISERROR('El monto debe ser mayor a cero.', 16, 1);
        RETURN;
    END

    IF @Mes NOT BETWEEN 1 AND 12
    BEGIN
        RAISERROR('El mes debe estar entre 1 y 12.', 16, 1);
        RETURN;
    END

    IF @Anio < 2020
    BEGIN
        RAISERROR('El año no es válido.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM Deuda
        WHERE IdPuesto = @IdPuesto
          AND IdTipoDeuda = @IdTipoDeuda
          AND Mes = @Mes
          AND Anio = @Anio
    )
    BEGIN
        RAISERROR('Ya existe esta deuda para ese periodo.', 16, 1);
        RETURN;
    END

    INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, FechaVencimiento, Estado)
    VALUES (@IdPuesto, @IdTipoDeuda, @Descripcion, @Monto, @Mes, @Anio, @FechaVencimiento, 'Pendiente');
END
GO

CREATE OR ALTER PROCEDURE sp_GenerarAlquilerMensual
    @Mes INT,
    @Anio INT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TipoAlquiler INT;

    SELECT @TipoAlquiler = IdTipoDeuda
    FROM TipoDeuda
    WHERE Nombre = 'Alquiler' AND Activo = 1;

    IF @TipoAlquiler IS NULL
    BEGIN
        RAISERROR('No existe el tipo de deuda Alquiler.', 16, 1);
        RETURN;
    END

    INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, Estado)
    SELECT 
        p.IdPuesto,
        @TipoAlquiler,
        'Alquiler mensual',
        p.MontoAlquiler,
        @Mes,
        @Anio,
        'Pendiente'
    FROM Puesto p
    WHERE p.Activo = 1
      AND p.IdSocio IS NOT NULL
      AND NOT EXISTS (
            SELECT 1
            FROM Deuda d
            WHERE d.IdPuesto = p.IdPuesto
              AND d.IdTipoDeuda = @TipoAlquiler
              AND d.Mes = @Mes
              AND d.Anio = @Anio
      );
END
GO

CREATE OR ALTER PROCEDURE sp_AplicarMora
    @IdDeuda INT,
    @MontoMora DECIMAL(10,2),
    @IdUsuario INT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM Deuda WHERE IdDeuda = @IdDeuda)
    BEGIN
        RAISERROR('La deuda no existe.', 16, 1);
        RETURN;
    END

    IF (SELECT Estado FROM Deuda WHERE IdDeuda = @IdDeuda) = 'Pagado'
    BEGIN
        RAISERROR('No se puede aplicar mora a una deuda pagada.', 16, 1);
        RETURN;
    END

    IF @MontoMora < 0
    BEGIN
        RAISERROR('El monto de la mora no puede ser negativo.', 16, 1);
        RETURN;
    END

    UPDATE Deuda
    SET Mora = @MontoMora,
        FechaAplicacionMora = GETDATE(),
        UsuarioAplicaMora = @IdUsuario
    WHERE IdDeuda = @IdDeuda;
END
GO

CREATE OR ALTER PROCEDURE sp_ListarDeudas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda,
        d.IdPuesto,
        d.IdTipoDeuda,
        p.Numero AS NumeroPuesto,
        td.Nombre AS NombreTipoDeuda,
        d.Descripcion, 
        d.Monto,
        ISNULL(d.Mora, 0) AS Mora,
        (d.Monto + ISNULL(d.Mora, 0)) AS MontoTotal,
        d.Mes, 
        d.Anio,
        d.FechaVencimiento,
        d.Estado 
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    INNER JOIN TipoDeuda td ON d.IdTipoDeuda = td.IdTipoDeuda
    ORDER BY d.Anio DESC, d.Mes DESC, p.Numero;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerDeudaPorId
    @IdDeuda INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda, d.IdPuesto, d.IdTipoDeuda, 
        p.Numero AS NumeroPuesto, 
        td.Nombre AS NombreTipoDeuda,
        d.Descripcion, 
        d.Monto,
        ISNULL(d.Mora, 0) AS Mora,
        (d.Monto + ISNULL(d.Mora, 0)) AS MontoTotal,
        d.Mes, d.Anio, d.FechaVencimiento, d.Estado
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    INNER JOIN TipoDeuda td ON d.IdTipoDeuda = td.IdTipoDeuda
    WHERE d.IdDeuda = @IdDeuda;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerDeudasPorPuesto
    @IdPuesto INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda, d.IdPuesto, d.IdTipoDeuda, 
        p.Numero AS NumeroPuesto, 
        td.Nombre AS NombreTipoDeuda,
        d.Descripcion, 
        d.Monto,
        ISNULL(d.Mora, 0) AS Mora,
        (d.Monto + ISNULL(d.Mora, 0)) AS MontoTotal,
        d.Mes, d.Anio, d.FechaVencimiento, d.Estado
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    INNER JOIN TipoDeuda td ON d.IdTipoDeuda = td.IdTipoDeuda
    WHERE d.IdPuesto = @IdPuesto
    ORDER BY d.Anio DESC, d.Mes DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerDeudasPorSocio
    @IdSocio INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda,
        d.IdPuesto,
        p.Numero AS NumeroPuesto,
        d.IdTipoDeuda,
        td.Nombre AS NombreTipoDeuda,
        d.Descripcion,
        d.Monto,
        ISNULL(d.Mora, 0) AS Mora,
        (d.Monto + ISNULL(d.Mora, 0)) AS MontoTotal,
        d.Mes,
        d.Anio,
        d.FechaVencimiento,
        d.Estado
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    INNER JOIN TipoDeuda td ON d.IdTipoDeuda = td.IdTipoDeuda
    WHERE p.IdSocio = @IdSocio
    ORDER BY p.Numero, d.FechaVencimiento;
END
GO

CREATE OR ALTER PROCEDURE sp_DeudasPendientes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda, d.IdPuesto, d.IdTipoDeuda, 
        p.Numero AS NumeroPuesto, 
        td.Nombre AS NombreTipoDeuda,
        d.Descripcion, 
        d.Monto,
        ISNULL(d.Mora, 0) AS Mora,
        (d.Monto + ISNULL(d.Mora, 0)) AS MontoTotal,
        d.Mes, d.Anio, d.FechaVencimiento, d.Estado
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    INNER JOIN TipoDeuda td ON d.IdTipoDeuda = td.IdTipoDeuda
    WHERE d.Estado = 'Pendiente'
    ORDER BY d.FechaVencimiento;
END
GO

CREATE OR ALTER PROCEDURE sp_ReporteDeudasPendientes
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda,
        p.Numero AS Puesto,
        td.Nombre AS TipoDeuda,
        d.Descripcion,
        d.Monto,
        ISNULL(d.Mora, 0) AS Mora,
        (d.Monto + ISNULL(d.Mora, 0)) AS MontoTotal,
        d.Mes,
        d.Anio,
        d.FechaVencimiento,
        d.Estado
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    INNER JOIN TipoDeuda td ON d.IdTipoDeuda = td.IdTipoDeuda
    WHERE d.Estado = 'Pendiente'
    ORDER BY d.FechaVencimiento;
END
GO

CREATE OR ALTER PROCEDURE sp_ReporteDeudasPagadas
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        d.IdDeuda,
        p.Numero AS Puesto,
        s.Nombre AS Socio,
        d.Monto,
        d.Mes,
        d.Anio,
        d.Estado
    FROM Deuda d
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    LEFT JOIN Socio s ON p.IdSocio = s.IdSocio
    WHERE d.Estado = 'Pagado'
    ORDER BY d.Anio DESC, d.Mes DESC;
END
GO

-- STORED PROCEDURES - PAGO


CREATE OR ALTER PROCEDURE sp_RegistrarPago
    @IdDeuda INT,
    @Monto DECIMAL(10,2),
    @MetodoPago NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @MontoDeuda DECIMAL(10,2);
    DECLARE @Mora DECIMAL(10,2);
    DECLARE @MontoTotal DECIMAL(10,2);
    DECLARE @Estado NVARCHAR(20);
    DECLARE @NumeroRecibo NVARCHAR(30);

    SELECT 
        @MontoDeuda = Monto,
        @Mora = ISNULL(Mora, 0),
        @Estado = Estado
    FROM Deuda
    WHERE IdDeuda = @IdDeuda;

    IF @MontoDeuda IS NULL
    BEGIN
        RAISERROR('La deuda no existe.', 16, 1);
        RETURN;
    END

    IF @Estado = 'Pagado'
    BEGIN
        RAISERROR('La deuda ya está pagada.', 16, 1);
        RETURN;
    END

    SET @MontoTotal = @MontoDeuda + @Mora;

    IF @Monto <> @MontoTotal
    BEGIN
        DECLARE @Mensaje NVARCHAR(200) = 'El monto debe ser exacto. Total a pagar (incluye mora): ' + CAST(@MontoTotal AS NVARCHAR(20));
        RAISERROR(@Mensaje, 16, 1);
        RETURN;
    END

    SET @NumeroRecibo = 'REC-' + FORMAT(GETDATE(), 'yyyyMMddHHmmss') + '-' + CAST(@IdDeuda AS NVARCHAR);

    INSERT INTO Pago (IdDeuda, Monto, Fecha, NumeroRecibo, MetodoPago)
    VALUES (@IdDeuda, @Monto, GETDATE(), @NumeroRecibo, @MetodoPago);

    UPDATE Deuda
    SET Estado = 'Pagado'
    WHERE IdDeuda = @IdDeuda;
END
GO

CREATE OR ALTER PROCEDURE sp_ListarPagos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        pa.IdPago, pa.IdDeuda, d.Descripcion AS ConceptoDeuda, 
        pa.Monto, pa.Fecha, pa.NumeroRecibo, pa.MetodoPago
    FROM Pago pa
    INNER JOIN Deuda d ON pa.IdDeuda = d.IdDeuda
    ORDER BY pa.Fecha DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_ReportePagosPorPuesto
    @IdPuesto INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        pa.IdPago,          
        pa.IdDeuda,    
        d.Descripcion AS ConceptoDeuda,
        pa.Monto,           
        pa.Fecha,          
        pa.NumeroRecibo,   
        pa.MetodoPago
    FROM Pago pa
    INNER JOIN Deuda d ON pa.IdDeuda = d.IdDeuda
    WHERE d.IdPuesto = @IdPuesto
    ORDER BY pa.Fecha DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerBoletaPago
    @IdDeuda INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        pa.NumeroRecibo,
        pa.Fecha,  
        pa.Monto,
        pa.MetodoPago, 
        s.Nombre AS NombreSocio,
        p.Numero AS NumeroPuesto,
        d.Descripcion AS ConceptoDeuda
    FROM Deuda d
    INNER JOIN Pago pa ON d.IdDeuda = pa.IdDeuda
    INNER JOIN Puesto p ON d.IdPuesto = p.IdPuesto
    LEFT JOIN Socio s ON p.IdSocio = s.IdSocio
    WHERE d.IdDeuda = @IdDeuda 
      AND d.Estado = 'Pagado';
END
GO

CREATE OR ALTER PROCEDURE sp_ReporteRecaudadoPorRango
    @FechaInicio DATE,
    @FechaFin DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        ISNULL(SUM(Monto), 0) AS TotalRecaudado
    FROM Pago
    WHERE Fecha >= @FechaInicio
      AND Fecha < DATEADD(DAY, 1, @FechaFin);
END
GO

-- STORED PROCEDURES - INGRESO DIARIO


CREATE OR ALTER PROCEDURE sp_RegistrarIngreso
    @IdPuesto INT,
    @Fecha DATE,
    @Monto DECIMAL(10,2),
    @IdUsuario INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1
        FROM Puesto
        WHERE IdPuesto = @IdPuesto
          AND Activo = 1
    )
    BEGIN
        RAISERROR('El puesto no existe o está inactivo.', 16, 1);
        RETURN;
    END

    IF EXISTS (
        SELECT 1
        FROM IngresoDiario
        WHERE IdPuesto = @IdPuesto
          AND Fecha = @Fecha
    )
    BEGIN
        RAISERROR('Ya existe un ingreso registrado para este puesto en esa fecha.', 16, 1);
        RETURN;
    END

    INSERT INTO IngresoDiario (IdPuesto, Fecha, Monto, IdUsuario)
    VALUES (@IdPuesto, @Fecha, @Monto, @IdUsuario);
END
GO

CREATE OR ALTER PROCEDURE sp_ListarIngresos
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        i.IdIngreso, i.IdPuesto, p.Numero AS NumeroPuesto, 
        i.Fecha, i.Monto, i.FechaRegistro, i.IdUsuario, u.Username AS NombreUsuario
    FROM IngresoDiario i
    INNER JOIN Puesto p ON i.IdPuesto = p.IdPuesto
    LEFT JOIN Usuario u ON i.IdUsuario = u.IdUsuario
    ORDER BY i.Fecha DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_ObtenerIngresosPorSocio
    @IdSocio INT,
    @FechaInicio DATE = NULL,
    @FechaFin DATE = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @FechaInicio IS NULL SET @FechaInicio = CAST(GETDATE() AS DATE);
    IF @FechaFin IS NULL SET @FechaFin = CAST(GETDATE() AS DATE);

    SELECT 
        p.Numero AS NumeroPuesto,
        i.IdIngreso,
        i.IdPuesto,
        i.Fecha,
        i.Monto,
        i.FechaRegistro
    FROM Puesto p
    INNER JOIN IngresoDiario i ON p.IdPuesto = i.IdPuesto
    WHERE p.IdSocio = @IdSocio
      AND i.Fecha BETWEEN @FechaInicio AND @FechaFin
    ORDER BY p.Numero, i.Fecha DESC;
END
GO

CREATE OR ALTER PROCEDURE sp_ReporteIngresosPorRango
    @FechaInicio DATE,
    @FechaFin DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        ISNULL(SUM(i.Monto), 0) AS TotalIngresos
    FROM IngresoDiario i
    WHERE i.Fecha >= @FechaInicio
      AND i.Fecha < DATEADD(DAY, 1, @FechaFin);
END
GO

CREATE OR ALTER PROCEDURE sp_ReporteIngresosPorPuesto
    @FechaInicio DATE,
    @FechaFin DATE
AS
BEGIN
    SET NOCOUNT ON;
    SELECT 
        p.Numero AS Puesto,
        ISNULL(SUM(i.Monto), 0) AS TotalIngresos
    FROM Puesto p
    LEFT JOIN IngresoDiario i 
        ON p.IdPuesto = i.IdPuesto
        AND i.Fecha >= @FechaInicio
        AND i.Fecha < DATEADD(DAY, 1, @FechaFin)
    GROUP BY p.Numero
    ORDER BY TotalIngresos DESC;
END
GO



-- DATOS INICIALES


-- Tipos de Deuda
INSERT INTO TipoDeuda (Nombre, MontoBase, Activo) VALUES
('Alquiler', NULL, 1),
('Luz', 50, 1),
('Agua', 30, 1),
('Limpieza', 25, 1),
('Vigilancia', 20, 1);
GO

-- Usuario Admin 
INSERT INTO Usuario (Username, Password, Rol, IdSocio, Activo) VALUES
('admin', 'admin123', 'Admin', NULL, 1);
GO


--  Socios con Usuarios Vinculados
DECLARE @IdUsuario1 INT, @IdSocio1 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio1',
    @Password = 'socio123',
    @NombreSocio = 'Juan Carlos Pérez López',
    @DNI = '70111111',
    @Telefono = '999111111',
    @IdSocioOut = @IdSocio1 OUTPUT,
    @IdUsuarioOut = @IdUsuario1 OUTPUT;

DECLARE @IdUsuario2 INT, @IdSocio2 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio2',
    @Password = 'socio123',
    @NombreSocio = 'María Elena García Torres',
    @DNI = '70222222',
    @Telefono = '999222222',
    @IdSocioOut = @IdSocio2 OUTPUT,
    @IdUsuarioOut = @IdUsuario2 OUTPUT;

DECLARE @IdUsuario3 INT, @IdSocio3 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio3',
    @Password = 'socio123',
    @NombreSocio = 'Carlos Alberto Rodríguez',
    @DNI = '70333333',
    @Telefono = '999333333',
    @IdSocioOut = @IdSocio3 OUTPUT,
    @IdUsuarioOut = @IdUsuario3 OUTPUT;

DECLARE @IdUsuario4 INT, @IdSocio4 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio4',
    @Password = 'socio123',
    @NombreSocio = 'Ana Patricia Martínez',
    @DNI = '70444444',
    @Telefono = '999444444',
    @IdSocioOut = @IdSocio4 OUTPUT,
    @IdUsuarioOut = @IdUsuario4 OUTPUT;

DECLARE @IdUsuario5 INT, @IdSocio5 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio5',
    @Password = 'socio123',
    @NombreSocio = 'Luis Fernando Hernández',
    @DNI = '70555555',
    @Telefono = '999555555',
    @IdSocioOut = @IdSocio5 OUTPUT,
    @IdUsuarioOut = @IdUsuario5 OUTPUT;

DECLARE @IdUsuario6 INT, @IdSocio6 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio6',
    @Password = 'socio123',
    @NombreSocio = 'Rosa María Sánchez',
    @DNI = '70666666',
    @Telefono = '999666666',
    @IdSocioOut = @IdSocio6 OUTPUT,
    @IdUsuarioOut = @IdUsuario6 OUTPUT;

DECLARE @IdUsuario7 INT, @IdSocio7 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio7',
    @Password = 'socio123',
    @NombreSocio = 'Pedro Antonio González',
    @DNI = '70777777',
    @Telefono = '999777777',
    @IdSocioOut = @IdSocio7 OUTPUT,
    @IdUsuarioOut = @IdUsuario7 OUTPUT;

DECLARE @IdUsuario8 INT, @IdSocio8 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio8',
    @Password = 'socio123',
    @NombreSocio = 'Laura Isabel Ramírez',
    @DNI = '70888888',
    @Telefono = '999888888',
    @IdSocioOut = @IdSocio8 OUTPUT,
    @IdUsuarioOut = @IdUsuario8 OUTPUT;

DECLARE @IdUsuario9 INT, @IdSocio9 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio9',
    @Password = 'socio123',
    @NombreSocio = 'Miguel Ángel Torres',
    @DNI = '70999999',
    @Telefono = '999999999',
    @IdSocioOut = @IdSocio9 OUTPUT,
    @IdUsuarioOut = @IdUsuario9 OUTPUT;

DECLARE @IdUsuario10 INT, @IdSocio10 INT;
EXEC sp_CrearUsuarioConSocio
    @Username = 'socio10',
    @Password = 'socio123',
    @NombreSocio = 'Carmen Beatriz Flores',
    @DNI = '71000000',
    @Telefono = '998000000',
    @IdSocioOut = @IdSocio10 OUTPUT,
    @IdUsuarioOut = @IdUsuario10 OUTPUT;
GO


-- Puestos 

INSERT INTO Puesto (Numero, Metraje, Ubicacion, Giro, MontoAlquiler, IdSocio) VALUES
('P01', 10.5, 'Zona Norte', 'Comida', 250, 1),
('P02', 12.0, 'Zona Norte', 'Ropa', 260, 2),
('P03', 8.5, 'Zona Este', 'Tecnología', 300, 3),
('P04', 15.0, 'Zona Sur', 'Carnes', 320, 4),
('P05', 9.0, 'Zona Oeste', 'Frutas', 200, 5),
('P06', 11.0, 'Centro', 'Verduras', 210, 6),
('P07', 14.0, 'Centro', 'Lácteos', 280, 7),
('P08', 10.0, 'Centro', 'Bebidas', 190, 8),
('P09', 13.0, 'Centro', 'Panadería', 230, 9),
('P10', 7.5, 'Centro', 'Juguetes', 180, 10),
('P11', 10.0, 'Zona Norte', 'Zapatos', 240, 1),
('P12', 12.0, 'Zona Norte', 'Accesorios', 250, 2),
('P13', 8.0, 'Zona Este', 'Celulares', 310, NULL),
('P14', 15.0, 'Zona Sur', 'Electrónica', 350, NULL),
('P15', 9.0, 'Zona Oeste', 'Hierbas', 190, NULL);
GO

-- historial inicial para puestos asignados
INSERT INTO HistorialPuesto (IdPuesto, IdSocio, FechaInicio)
SELECT IdPuesto, IdSocio, '2026-01-01'
FROM Puesto
WHERE IdSocio IS NOT NULL;
GO

-- GENERAR DATOS HISTÓRICOS: ENERO A MAYO 2026

-- ENERO 2026
EXEC sp_GenerarAlquilerMensual @Mes = 1, @Anio = 2026;
GO

-- Deudas de servicios Enero
INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, FechaVencimiento, Estado) VALUES
(1, 2, 'Luz Enero', 75, 1, 2026, '2026-01-31', 'Pagado'),
(1, 3, 'Agua Enero', 55, 1, 2026, '2026-01-31', 'Pagado'),
(2, 2, 'Luz Enero', 70, 1, 2026, '2026-01-31', 'Pagado'),
(2, 3, 'Agua Enero', 50, 1, 2026, '2026-01-31', 'Pagado'),
(3, 2, 'Luz Enero', 85, 1, 2026, '2026-01-31', 'Pagado'),
(3, 3, 'Agua Enero', 65, 1, 2026, '2026-01-31', 'Pagado'),
(4, 2, 'Luz Enero', 95, 1, 2026, '2026-01-31', 'Pagado'),
(5, 2, 'Luz Enero', 60, 1, 2026, '2026-01-31', 'Pagado');
GO

-- Pagos de Enero (Alquileres + Servicios)
DECLARE @IdDeudaTemp INT;

-- Pagar alquileres de Enero
UPDATE Deuda SET Estado = 'Pagado' WHERE Mes = 1 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto IN (1,2,3,4,5,6,7,8,9,10);

-- Registrar pagos de Enero
INSERT INTO Pago (IdDeuda, Monto, Fecha, NumeroRecibo, MetodoPago)
SELECT IdDeuda, Monto, '2026-01-28', 'REC-ENE-' + CAST(IdDeuda AS VARCHAR), 'Efectivo'
FROM Deuda WHERE Mes = 1 AND Anio = 2026 AND Estado = 'Pagado';
GO

-- Ingresos diarios de Enero (10 días de muestra)
INSERT INTO IngresoDiario (IdPuesto, Fecha, Monto) VALUES
-- Día 1
(1, '2026-01-05', 480), (2, '2026-01-05', 420), (3, '2026-01-05', 580), (4, '2026-01-05', 680), (5, '2026-01-05', 280),
(6, '2026-01-05', 350), (7, '2026-01-05', 450), (8, '2026-01-05', 320), (9, '2026-01-05', 380), (10, '2026-01-05', 290),
-- Día 2
(1, '2026-01-06', 500), (2, '2026-01-06', 440), (3, '2026-01-06', 600), (4, '2026-01-06', 700), (5, '2026-01-06', 300),
-- Día 3
(1, '2026-01-10', 490), (2, '2026-01-10', 430), (3, '2026-01-10', 590), (4, '2026-01-10', 690), (5, '2026-01-10', 290),
-- Día 4
(1, '2026-01-15', 510), (2, '2026-01-15', 450), (3, '2026-01-15', 610), (4, '2026-01-15', 710), (5, '2026-01-15', 310),
-- Día 5
(1, '2026-01-20', 495), (2, '2026-01-20', 445), (3, '2026-01-20', 595), (4, '2026-01-20', 695), (5, '2026-01-20', 295);
GO


-- FEBRERO 2026
EXEC sp_GenerarAlquilerMensual @Mes = 2, @Anio = 2026;
GO

-- Deudas de servicios Febrero
INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, FechaVencimiento, Estado) VALUES
(1, 2, 'Luz Febrero', 78, 2, 2026, '2026-02-28', 'Pagado'),
(1, 3, 'Agua Febrero', 58, 2, 2026, '2026-02-28', 'Pagado'),
(2, 2, 'Luz Febrero', 72, 2, 2026, '2026-02-28', 'Pagado'),
(2, 3, 'Agua Febrero', 52, 2, 2026, '2026-02-28', 'Pagado'),
(3, 2, 'Luz Febrero', 88, 2, 2026, '2026-02-28', 'Pagado'),
(3, 3, 'Agua Febrero', 68, 2, 2026, '2026-02-28', 'Pagado'),
(4, 2, 'Luz Febrero', 98, 2, 2026, '2026-02-28', 'Pagado');
GO

-- Pagar alquileres de Febrero
UPDATE Deuda SET Estado = 'Pagado' WHERE Mes = 2 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto IN (1,2,3,4,5,6,7,8,9,10);

-- Registrar pagos de Febrero
INSERT INTO Pago (IdDeuda, Monto, Fecha, NumeroRecibo, MetodoPago)
SELECT IdDeuda, Monto, '2026-02-28', 'REC-FEB-' + CAST(IdDeuda AS VARCHAR), 'Transferencia'
FROM Deuda WHERE Mes = 2 AND Anio = 2026 AND Estado = 'Pagado';
GO

-- Ingresos diarios de Febrero
INSERT INTO IngresoDiario (IdPuesto, Fecha, Monto) VALUES
(1, '2026-02-03', 505), (2, '2026-02-03', 455), (3, '2026-02-03', 605), (4, '2026-02-03', 705), (5, '2026-02-03', 305),
(1, '2026-02-10', 515), (2, '2026-02-10', 465), (3, '2026-02-10', 615), (4, '2026-02-10', 715), (5, '2026-02-10', 315),
(1, '2026-02-15', 510), (2, '2026-02-15', 460), (3, '2026-02-15', 610), (4, '2026-02-15', 710), (5, '2026-02-15', 310),
(1, '2026-02-20', 520), (2, '2026-02-20', 470), (3, '2026-02-20', 620), (4, '2026-02-20', 720), (5, '2026-02-20', 320),
(1, '2026-02-25', 525), (2, '2026-02-25', 475), (3, '2026-02-25', 625), (4, '2026-02-25', 725), (5, '2026-02-25', 325);
GO

-- MARZO 2026
EXEC sp_GenerarAlquilerMensual @Mes = 3, @Anio = 2026;
GO

-- Deudas de servicios Marzo
INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, FechaVencimiento, Estado) VALUES
(1, 2, 'Luz Marzo', 82, 3, 2026, '2026-03-31', 'Pagado'),
(1, 3, 'Agua Marzo', 62, 3, 2026, '2026-03-31', 'Pagado'),
(2, 2, 'Luz Marzo', 77, 3, 2026, '2026-03-31', 'Pagado'),
(2, 3, 'Agua Marzo', 57, 3, 2026, '2026-03-31', 'Pagado'),
(3, 2, 'Luz Marzo', 92, 3, 2026, '2026-03-31', 'Pagado'),
(3, 3, 'Agua Marzo', 72, 3, 2026, '2026-03-31', 'Pagado');
GO

-- Pagar alquileres de Marzo (algunos con mora)
UPDATE Deuda SET Estado = 'Pagado' WHERE Mes = 3 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto IN (1,2,3,4,5,6,7,8,9);

-- Aplicar mora al puesto 10 (no pagó a tiempo)
UPDATE Deuda SET Mora = 20 WHERE Mes = 3 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto = 10;

-- Registrar pagos de Marzo
INSERT INTO Pago (IdDeuda, Monto, Fecha, NumeroRecibo, MetodoPago)
SELECT IdDeuda, Monto, '2026-03-30', 'REC-MAR-' + CAST(IdDeuda AS VARCHAR), 'Yape'
FROM Deuda WHERE Mes = 3 AND Anio = 2026 AND Estado = 'Pagado';
GO

-- Ingresos diarios de Marzo
INSERT INTO IngresoDiario (IdPuesto, Fecha, Monto) VALUES
(1, '2026-03-05', 530), (2, '2026-03-05', 480), (3, '2026-03-05', 630), (4, '2026-03-05', 730), (5, '2026-03-05', 330),
(6, '2026-03-05', 360), (7, '2026-03-05', 460), (8, '2026-03-05', 330), (9, '2026-03-05', 390), (10, '2026-03-05', 300),
(1, '2026-03-12', 540), (2, '2026-03-12', 490), (3, '2026-03-12', 640), (4, '2026-03-12', 740), (5, '2026-03-12', 340),
(1, '2026-03-18', 535), (2, '2026-03-18', 485), (3, '2026-03-18', 635), (4, '2026-03-18', 735), (5, '2026-03-18', 335),
(1, '2026-03-25', 545), (2, '2026-03-25', 495), (3, '2026-03-25', 645), (4, '2026-03-25', 745), (5, '2026-03-25', 345);
GO

-- ABRIL 2026
EXEC sp_GenerarAlquilerMensual @Mes = 4, @Anio = 2026;
GO

-- Deudas de servicios Abril
INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, FechaVencimiento, Estado) VALUES
(1, 2, 'Luz Abril', 85, 4, 2026, '2026-04-30', 'Pagado'),
(1, 3, 'Agua Abril', 65, 4, 2026, '2026-04-30', 'Pagado'),
(2, 2, 'Luz Abril', 80, 4, 2026, '2026-04-30', 'Pagado'),
(2, 3, 'Agua Abril', 60, 4, 2026, '2026-04-30', 'Pagado'),
(3, 2, 'Luz Abril', 95, 4, 2026, '2026-04-30', 'Pagado'),
(3, 3, 'Agua Abril', 75, 4, 2026, '2026-04-30', 'Pagado'),
(4, 2, 'Luz Abril', 100, 4, 2026, '2026-04-30', 'Pagado'),
(5, 2, 'Luz Abril', 65, 4, 2026, '2026-04-30', 'Pagado');
GO

-- Pagar alquileres de Abril
UPDATE Deuda SET Estado = 'Pagado' WHERE Mes = 4 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto IN (1,2,3,4,5,6,7,8);

-- Aplicar mora a puestos 9 y 10
UPDATE Deuda SET Mora = 25 WHERE Mes = 4 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto IN (9,10);

-- Registrar pagos de Abril
INSERT INTO Pago (IdDeuda, Monto, Fecha, NumeroRecibo, MetodoPago)
SELECT IdDeuda, Monto, '2026-04-28', 'REC-ABR-' + CAST(IdDeuda AS VARCHAR), 'Efectivo'
FROM Deuda WHERE Mes = 4 AND Anio = 2026 AND Estado = 'Pagado';
GO

-- Ingresos diarios de Abril
INSERT INTO IngresoDiario (IdPuesto, Fecha, Monto) VALUES
(1, '2026-04-02', 550), (2, '2026-04-02', 500), (3, '2026-04-02', 650), (4, '2026-04-02', 750), (5, '2026-04-02', 350),
(1, '2026-04-08', 560), (2, '2026-04-08', 510), (3, '2026-04-08', 660), (4, '2026-04-08', 760), (5, '2026-04-08', 360),
(1, '2026-04-15', 555), (2, '2026-04-15', 505), (3, '2026-04-15', 655), (4, '2026-04-15', 755), (5, '2026-04-15', 355),
(1, '2026-04-22', 565), (2, '2026-04-22', 515), (3, '2026-04-22', 665), (4, '2026-04-22', 765), (5, '2026-04-22', 365),
(1, '2026-04-29', 570), (2, '2026-04-29', 520), (3, '2026-04-29', 670), (4, '2026-04-29', 770), (5, '2026-04-29', 370);
GO

-- MAYO 2026 (Mes actual - deudas pendientes)
EXEC sp_GenerarAlquilerMensual @Mes = 5, @Anio = 2026;
GO

-- Deudas de servicios Mayo (algunas pagadas, otras pendientes)
INSERT INTO Deuda (IdPuesto, IdTipoDeuda, Descripcion, Monto, Mes, Anio, FechaVencimiento, Estado) VALUES
(1, 2, 'Luz Mayo', 88, 5, 2026, '2026-05-31', 'Pagado'),
(1, 3, 'Agua Mayo', 68, 5, 2026, '2026-05-31', 'Pagado'),
(2, 2, 'Luz Mayo', 83, 5, 2026, '2026-05-31', 'Pagado'),
(2, 3, 'Agua Mayo', 63, 5, 2026, '2026-05-31', 'Pagado'),
(3, 2, 'Luz Mayo', 98, 5, 2026, '2026-05-31', 'Pagado'),
(3, 3, 'Agua Mayo', 78, 5, 2026, '2026-05-31', 'Pagado'),
(4, 2, 'Luz Mayo', 103, 5, 2026, '2026-05-31', 'Pendiente'),
(5, 2, 'Luz Mayo', 68, 5, 2026, '2026-05-31', 'Pendiente'),
(6, 2, 'Luz Mayo', 70, 5, 2026, '2026-05-31', 'Pendiente');
GO

-- Pagar solo algunos alquileres de Mayo
UPDATE Deuda SET Estado = 'Pagado' WHERE Mes = 5 AND Anio = 2026 AND IdTipoDeuda = 1 AND IdPuesto IN (1,2,3,4,5);

-- Registrar pagos de Mayo
INSERT INTO Pago (IdDeuda, Monto, Fecha, NumeroRecibo, MetodoPago)
SELECT IdDeuda, Monto, '2026-05-15', 'REC-MAY-' + CAST(IdDeuda AS VARCHAR), 'Yape'
FROM Deuda WHERE Mes = 5 AND Anio = 2026 AND Estado = 'Pagado';
GO

-- Ingresos diarios de Mayo (hasta hoy)
INSERT INTO IngresoDiario (IdPuesto, Fecha, Monto) VALUES
-- Semana 1
(1, '2026-05-01', 575), (2, '2026-05-01', 525), (3, '2026-05-01', 675), (4, '2026-05-01', 775), (5, '2026-05-01', 375),
(6, '2026-05-01', 370), (7, '2026-05-01', 470), (8, '2026-05-01', 340), (9, '2026-05-01', 400), (10, '2026-05-01', 310),
(11, '2026-05-01', 420), (12, '2026-05-01', 390),
-- Semana 2
(1, '2026-05-05', 580), (2, '2026-05-05', 530), (3, '2026-05-05', 680), (4, '2026-05-05', 780), (5, '2026-05-05', 380),
(1, '2026-05-08', 585), (2, '2026-05-08', 535), (3, '2026-05-08', 685), (4, '2026-05-08', 785), (5, '2026-05-08', 385),
-- Semana 3
(1, '2026-05-12', 590), (2, '2026-05-12', 540), (3, '2026-05-12', 690), (4, '2026-05-12', 790), (5, '2026-05-12', 390),
(1, '2026-05-15', 595), (2, '2026-05-15', 545), (3, '2026-05-15', 695), (4, '2026-05-15', 795), (5, '2026-05-15', 395),
-- Semana 4
(1, '2026-05-19', 600), (2, '2026-05-19', 550), (3, '2026-05-19', 700), (4, '2026-05-19', 800), (5, '2026-05-19', 400),
(1, '2026-05-22', 605), (2, '2026-05-22', 555), (3, '2026-05-22', 705), (4, '2026-05-22', 805), (5, '2026-05-22', 405),
(6, '2026-05-22', 385), (7, '2026-05-22', 485), (8, '2026-05-22', 355), (9, '2026-05-22', 415), (10, '2026-05-22', 325);
GO



select*from Usuario
select*from Puesto
select*from Socio
select*from deuda
