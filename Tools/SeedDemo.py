"""Adds fictional patients to a local screenshot database."""

import datetime as dt
import sqlite3
import sys


database = sys.argv[1]
visit_date = dt.date.today() + dt.timedelta(days=1)
while visit_date.weekday() >= 5:
    visit_date += dt.timedelta(days=1)

rows = [
    ("Крылов Максим Сергеевич", "1987-04-09", 3, visit_date, "10:30", "Active"),
    ("Морозова Елена Павловна", "1979-11-21", 6,
     visit_date + dt.timedelta(days=1), "11:00", "Active"),
    ("Сидоров Андрей Петрович", "1968-01-17", 4,
     visit_date + dt.timedelta(days=1), "13:30", "Cancelled"),
]

with sqlite3.connect(database) as connection:
    for name, birth_date, doctor_id, date, time, status in rows:
        found = connection.execute(
            "SELECT Id FROM Patients WHERE FullName = ? AND BirthDate = ?",
            (name, birth_date),
        ).fetchone()
        if found:
            continue
        patient_id = connection.execute(
            "INSERT INTO Patients(FullName, BirthDate) VALUES (?, ?)",
            (name, birth_date),
        ).lastrowid
        connection.execute(
            """INSERT INTO Appointments
               (PatientId, DoctorId, VisitDate, VisitTime, Status)
               VALUES (?, ?, ?, ?, ?)""",
            (patient_id, doctor_id, date.isoformat(), time, status),
        )

print("Added fictional demo entries to", database)
