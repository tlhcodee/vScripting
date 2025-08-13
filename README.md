# vScripting

vScripting, **Crab Game** için yazılmış basit bir metin dilidir. Oyuncular kendi komutlarını ve olaylarını yazarak oyun içinde özelleştirilmiş davranışlar ekleyebilir.

Bu sistem sayesinde oyun içinde tetiklenen olaylara kendi kodunuzu yazabilir, değişkenler tanımlayabilir, mesajlar gönderebilir veya kendi özel API fonksiyonlarınızı ekleyebilirsiniz.

---

## ✨ Özellikler
- Olay tabanlı betik sistemi (`olay` / `onEvent`)
- Birden fazla dil desteği (Türkçe ve İngilizce anahtar kelimeler)
- Global değişken tanımlama (`tam`, `string` vb.)
- Değişken manipülasyonu (`=`, `++`, `--`, `+=`, `-=`)
- Konsola veya oyun sohbetine yazdırma (`yazdir`, `print`)
- Kolay genişletilebilir komut yapısı

---

## 📂 Kurulum
1. **BepInEx** ile çalışan Crab Game'e vScripting DLL’ini ekleyin.
2. `BepInEx/config` klasöründe `.vs` uzantılı betik dosyanızı oluşturun.
3. Oyunu başlattığınızda betikler otomatik olarak yüklenecektir.

---

## 📜 Betik Sözdizimi

### 1. Olay Tanımlama
Bir olay, oyun içinde belirli bir durumda tetiklenir.
```plaintext
olay OyunBaslangici
    yazdir("Yeni round başladı!")
son
```

**Desteklenen olay örnekleri:**
- `OyunBaslangici` – Yeni round başladığında tetiklenir
- `OyuncuTag(taglayan, taglanan)` – Bir oyuncu başka bir oyuncuyu tag’ladığında tetiklenir

---

### 2. Parametreli Olaylar
Olaylara parametre ekleyebilir ve bunları mesaj içinde kullanabilirsiniz:
```plaintext
olay OyuncuTag(taglayan, taglanan)
    yazdir("Oyuncu " + taglayan + " tagladı " + taglanan)
son
```

---

### 3. Global Değişkenler
Global değişkenler `tam` (integer) anahtar kelimesi ile tanımlanır:
```plaintext
tam skor = 0
```

Değişkenler olaylar içinde değiştirilebilir:
```plaintext
olay OyunBaslangici
    skor++
    yazdir("Round başladı! Mevcut skor: " + skor)
son
```

Desteklenen işlemler:
- `=` → Atama
- `++` / `--` → Arttırma / Azaltma
- `+=` / `-=` → Belirli miktar arttırma / azaltma

---

### 4. Yazdırma Komutu
`yazdir` veya `print` komutu ile metinleri oyun sohbetine gönderebilirsiniz.
```plaintext
yazdir("Merhaba dünya!")
```

Değişkenleri + operatörü ile birleştirebilirsiniz:
```plaintext
yazdir("Skor: " + skor)
```

---

## 📌 Örnek Betik
```plaintext
tam round = 0
tam skor = 0

olay OyunBaslangici
    round++
    skor = 0
    yazdir("Yeni round başladı! Round: " + round)
son

olay OyuncuTag(taglayan, taglanan)
    skor++
    yazdir("Oyuncu " + taglayan + " tagladı " + taglanan + " | Skor: " + skor)
son
```

---

## 🔌 API Genişletme
Kendi C# kodunuzda özel API fonksiyonları ekleyebilirsiniz:
```csharp
engine.RegisterApi("teleport", (ctx, args) => {
    var player = FindPlayerByName(args[0]);
    player.TeleportTo(new Vector3(0, 5, 0));
});
```

Betik tarafında:
```plaintext
olay OyunBaslangici
    teleport("Oyuncu1")
son
```

---

## ⚠️ Hata Mesajları
- `Bilinmeyen komut: ...` → Yazdığınız komut tanımlı değil.
- `Değişken bulunamadı: ...` → Kullanılan değişken tanımlanmamış.
- `Geçersiz sözdizimi: ...` → Parantez veya tırnak yapısında hata var.
