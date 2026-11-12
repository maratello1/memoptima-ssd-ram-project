"""
MemOptima GUI - Графический комплекс оптимизации оперативной памяти и SSD
Автор проекта: Алиев Марат, группа РУП-8
Научный руководитель: Худошина Мария Андреевна
Дата: Ноябрь 2026 г.
"""

import sys
import os
import ctypes
from ctypes import wintypes
import subprocess
import tkinter as tk
from tkinter import ttk, messagebox, filedialog

class MEMORYSTATUSEX(ctypes.Structure):
    _fields_ = [
        ("dwLength", wintypes.DWORD),
        ("dwMemoryLoad", wintypes.DWORD),
        ("ullTotalPhys", ctypes.c_uint64),
        ("ullAvailPhys", ctypes.c_uint64),
        ("ullTotalPageFile", ctypes.c_uint64),
        ("ullAvailPageFile", ctypes.c_uint64),
        ("ullTotalVirtual", ctypes.c_uint64),
        ("ullAvailVirtual", ctypes.c_uint64),
        ("ullAvailExtendedVirtual", ctypes.c_uint64),
    ]

def get_memory_info():
    kernel32 = ctypes.windll.kernel32
    mem_status = MEMORYSTATUSEX()
    mem_status.dwLength = ctypes.sizeof(MEMORYSTATUSEX)
    if kernel32.GlobalMemoryStatusEx(ctypes.byref(mem_status)):
        total_gb = mem_status.ullTotalPhys / (1024**3)
        avail_gb = mem_status.ullAvailPhys / (1024**3)
        used_gb = total_gb - avail_gb
        load = mem_status.dwMemoryLoad
        return total_gb, used_gb, avail_gb, load
    return 0, 0, 0, 0

def optimize_ram_action():
    psapi = ctypes.windll.psapi
    kernel32 = ctypes.windll.kernel32
    
    # PROCESS_QUERY_INFORMATION | PROCESS_SET_QUOTA
    PROCESS_ALL_ACCESS = 0x1F0FFF
    
    # Enumerate processes
    pids = (wintypes.DWORD * 2048)()
    cb_needed = wintypes.DWORD()
    success_count = 0
    
    if psapi.EnumProcesses(ctypes.byref(pids), ctypes.sizeof(pids), ctypes.byref(cb_needed)):
        count = int(cb_needed.value / ctypes.sizeof(wintypes.DWORD))
        for i in range(count):
            pid = pids[i]
            if pid <= 4:
                continue
            h_process = kernel32.OpenProcess(0x0400 | 0x0100, False, pid)
            if h_process:
                if psapi.EmptyWorkingSet(h_process):
                    success_count += 1
                kernel32.CloseHandle(h_process)
    
    # Also trim our own process
    current_h = kernel32.GetCurrentProcess()
    kernel32.SetProcessWorkingSetSize(current_h, -1, -1)
    return success_count

def purge_caches_action():
    freed_bytes = 0
    temp_dir = os.environ.get("TEMP", "")
    local_app = os.environ.get("LOCALAPPDATA", "")
    
    targets = [
        temp_dir,
        os.path.join(local_app, "Discord", "Cache"),
        os.path.join(local_app, "Telegram Desktop", "tdata", "user_data", "cache"),
        os.path.join(local_app, "Google", "Chrome", "User Data", "Default", "Cache"),
        os.path.join(local_app, "Yandex", "YandexBrowser", "User Data", "Default", "Cache")
    ]
    
    for folder in targets:
        if os.path.exists(folder):
            for root, _, files in os.walk(folder):
                for f in files:
                    fp = os.path.join(root, f)
                    try:
                        sz = os.path.getsize(fp)
                        os.remove(fp)
                        freed_bytes += sz
                    except:
                        pass
    return freed_bytes / (1024 * 1024)

class MemOptimaApp(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title("MemOptima - Оптимизация RAM и SSD | Алиев Марат (РУП-8)")
        self.geometry("540x520")
        self.resizable(False, False)
        self.configure(bg="#1E1E2E")
        
        # Style
        self.style = ttk.Style(self)
        self.style.theme_use("clam")
        
        self.create_widgets()
        self.update_stats()

    def create_widgets(self):
        title_label = tk.Label(self, text="⚡ MemOptima System Optimizer", font=("Segoe UI", 16, "bold"), fg="#89B4FA", bg="#1E1E2E")
        title_label.pack(pady=(15, 5))
        
        sub_label = tk.Label(self, text="Программный ответ на кризис цен на SSD и RAM\nАвтор: Алиев Марат (РУП-8) | Науч. рук.: Худошина М.А.", font=("Segoe UI", 9), fg="#A6ADC8", bg="#1E1E2E")
        sub_label.pack(pady=(0, 15))
        
        # RAM Stats Frame
        stats_frame = tk.LabelFrame(self, text=" Состояние оперативной памяти (RAM) ", font=("Segoe UI", 10, "bold"), fg="#CDD6F4", bg="#181825", padx=15, pady=10)
        stats_frame.pack(fill="x", padx=20, pady=5)
        
        self.ram_progress = ttk.Progressbar(stats_frame, orient="horizontal", mode="determinate", length=460)
        self.ram_progress.pack(pady=5)
        
        self.ram_label = tk.Label(stats_frame, text="Загрузка RAM: 0%", font=("Segoe UI", 11), fg="#F38BA8", bg="#181825")
        self.ram_label.pack()
        
        self.details_label = tk.Label(stats_frame, text="Используется: 0 ГБ / Всего: 0 ГБ", font=("Segoe UI", 9), fg="#BAC2DE", bg="#181825")
        self.details_label.pack()

        # Action Buttons Frame
        btn_frame = tk.Frame(self, bg="#1E1E2E")
        btn_frame.pack(fill="x", padx=20, pady=15)
        
        btn_opt_ram = tk.Button(btn_frame, text="🧹 Очистить оперативную память (Win32 API)", font=("Segoe UI", 10, "bold"), bg="#A6E3A1", fg="#11111B", height=2, cursor="hand2", command=self.on_clean_ram)
        btn_opt_ram.pack(fill="x", pady=4)
        
        btn_clean_cache = tk.Button(btn_frame, text="🗑️ Очистить скрытые кэши мессенджеров и браузеров", font=("Segoe UI", 10), bg="#F9E2AF", fg="#11111B", height=2, cursor="hand2", command=self.on_clean_cache)
        btn_clean_cache.pack(fill="x", pady=4)
        
        btn_compress = tk.Button(btn_frame, text="🗜️ Сжать тяжелую папку алгоритмом NTFS CompactOS", font=("Segoe UI", 10), bg="#89DCEB", fg="#11111B", height=2, cursor="hand2", command=self.on_compress_folder)
        btn_compress.pack(fill="x", pady=4)

        # Log label
        self.status_label = tk.Label(self, text="Готов к работе.", font=("Segoe UI", 9, "italic"), fg="#A6ADC8", bg="#1E1E2E")
        self.status_label.pack(side="bottom", pady=10)

    def update_stats(self):
        total, used, avail, load = get_memory_info()
        self.ram_progress["value"] = load
        self.ram_label.config(text=f"Загрузка RAM: {load}%", fg="#F38BA8" if load > 75 else "#A6E3A1")
        self.details_label.config(text=f"Занято: {used:.2f} ГБ | Свободно: {avail:.2f} ГБ | Всего: {total:.2f} ГБ")
        self.after(2000, self.update_stats)

    def on_clean_ram(self):
        tot, u_before, _, _ = get_memory_info()
        count = optimize_ram_action()
        tot, u_after, _, _ = get_memory_info()
        freed = max(0, (u_before - u_after) * 1024)
        self.status_label.config(text=f"Успех! Очищено {count} процессов. Высвобождено {freed:.1f} МБ ОЗУ.")
        messagebox.showinfo("MemOptima", f"Оптимизация завершена!\nОбработано процессов: {count}\nВысвобождено памяти: ~{freed:.0f} МБ")

    def on_clean_cache(self):
        freed_mb = purge_caches_action()
        self.status_label.config(text=f"Кэш очищен! Освобождено {freed_mb:.1f} МБ на SSD.")
        messagebox.showinfo("MemOptima", f"Очистка кэша завершена!\nОсвобождено места на накопителе: {freed_mb:.1f} МБ")

    def on_compress_folder(self):
        folder = filedialog.askdirectory(title="Выберите папку для сжатия (игры, программы)")
        if folder:
            self.status_label.config(text=f"Сжатие папки {os.path.basename(folder)}... Ожидайте.")
            self.update()
            try:
                cmd = f'compact.exe /C /S:"{folder}" /EXE:XPRESS8K /I /Q'
                subprocess.run(cmd, shell=True, check=True)
                messagebox.showinfo("MemOptima", f"Папка успешно сжата алгоритмом XPRESS8K!\nОсвобождено место на SSD без потери скорости.")
                self.status_label.config(text="Сжатие завершено успешно.")
            except Exception as e:
                messagebox.showerror("Ошибка сжатия", str(e))
                self.status_label.config(text="Ошибка при сжатии.")

if __name__ == "__main__":
    app = MemOptimaApp()
    app.mainloop()
