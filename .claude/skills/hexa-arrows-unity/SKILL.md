---
name: hexa-arrows-unity
description: Hexa Arrows adlı hexagon döndürme bulmaca oyununun eksiksiz oyun tasarımı, kuralları, algoritmaları ve Unity 3D mobil portu için teknik spesifikasyon. Unity/C# ile Hexa Arrows geliştirilirken, oyun mekaniği/level üretimi/buz mekaniği/renk sistemi hakkında herhangi bir soru veya implementasyon işi geldiğinde bu skill kullanılmalı. HTML prototipi reference/ klasöründedir ve davranış konusunda nihai kaynaktır (source of truth).
---

# Hexa Arrows — Unity Mobil Port Spesifikasyonu

Bu skill, Hexa Arrows oyununun HTML prototipinden Unity 3D mobil sürümüne birebir taşınması için gereken TÜM kuralları içerir. Davranış konusunda tereddütte kalırsan `reference/hexa-arrows-prototype.html` dosyasındaki implementasyon nihai kaynaktır — önce onu oku, prototipin yaptığını yap.

## 1. Oyun Konsepti (tek paragraf)

Hexagon taşlardan oluşan bir tahtada, taşların üzerinde renkli okların parçaları (segmentler) vardır. Oyuncunun tek etkileşimi bir taşa dokunmaktır; dokunulan taş saat yönünde 60° döner ve üzerindeki segment de onunla döner. Amaç, bir okun tüm segmentlerini kuyruktan uca kesintisiz bağlamaktır. Bağlanan ok otomatik olarak kendi yolu boyunca süzülüp head yönünde tahtadan çıkar. Çıkış yolunda başka bir okun segmenti varsa çarpıp geri döner ve engel kalkana kadar bekler. Tüm oklar çıkınca level biter.

## 2. Koordinat Sistemi ve Hex Geometrisi

**Flat-top (üst kenarı düz) hexagonlar, axial koordinat (q, r).**

- Hücre merkezi (prototip, y AŞAĞI): `x = 1.5·S·q`, `y = √3·S·(r + q/2)` (S = köşe yarıçapı)
- Komşu yönleri `DIRS` dizisi, indeks d → axial delta ve prototipteki açı (y aşağı):

| d | (dq,dr) | açı (y aşağı) | yön |
|---|---------|---------------|-----|
| 0 | (+1, 0) | 30°  | sağ-aşağı |
| 1 | ( 0,+1) | 90°  | aşağı |
| 2 | (−1,+1) | 150° | sol-aşağı |
| 3 | (−1, 0) | 210° | sol-yukarı |
| 4 | ( 0,−1) | 270° | yukarı |
| 5 | (+1,−1) | 330° | sağ-yukarı |

- Karşı yön: `opp(d) = (d+3) % 6`
- Kenar orta noktası: merkez + apothem·(cos a, sin a), `apothem = √3/2·S`, `a = 30° + 60°·d`
- Komşu merkezler arası mesafe: `√3·S` (her yönde eşit)

**⚠ Unity ekseni uyarısı:** Prototip SVG'dir, y ekseni AŞAĞI bakar ve "saat yönü" bu düzlemde açı ARTIŞIDIR. Unity'de y (veya 2D'de world up) YUKARI bakar. İki seçenek: (a) açıları `-a` olarak aynala ve dönüşü `-60°` uygula, veya (b) r eksenini ters çevir. Hangisini seçersen seç, "taşa dokun → EKRANDA saat yönünde 60° dönme" davranışı korunMALIdır ve `DIRS` tablosu ile açı eşlemesi tutarlı kalmalıdır. Bunu port başında birim testle sabitle.

## 3. Veri Modeli

**Hücre (Cell):** `{ q, r, arrowId, type: tail|mid|head, a, b, rot }`
- `a`, `b`: LOKAL kenar yönleri (0-5). Çözülmüş halde `rot = 0` iken lokal = dünya yönü.
- Dünya yönü her zaman: `world = (local + rot) % 6`. `rot` 0-5 arası; her dokunuş `rot = (rot+1) % 6`.
- `tail`: `b` = çıkış kenarı (`a` yok). Görsel: merkezde nokta + merkezden b kenar ortasına çizgi.
- `mid`: `a` = giriş kenarı, `b` = çıkış kenarı. Görsel: a kenar ortası → **MERKEZ** → b kenar ortası (V/kırık çizgi; köşe daima merkezde). Segmentler ASLA merkezden geçmeyen kestirme çizgi olamaz.
- `head`: `a` = giriş kenarı, `b` = uçuş yönü. Görsel: a kenar ortası → merkez → merkezden b yönüne kısa çizgi (uç noktası merkez + 0.62·apothem·b_yönü) + ok ucu üçgeni.
- Bir hücrede EN FAZLA bir segment olur. Segmentsiz taş tahtada yoktur (tahta = yalnızca segment taşıyan taşlar, düzgün hex kafese oturur; küme organik/delikli olabilir).

**Ok (Arrow):** `{ arrowId, palette, cells[] (kuyruk→head sıralı hücre listesi), len, exitDir (çözümdeki uçuş yönü), freezeAt (0=buzsuz, 1-3=eşik), state, exited, unfrozen }`
- `cells` dizisi DAİMA sıralıdır: `cells[0]` tail, `cells[len-1]` head, arası mid. Ardışık her hücre çifti axial komşudur (değişmez/invariant).

**Kritik ayrım — ok kimliği ≠ renk:** `arrowId` mantıksal kimliktir; `palette` (0-9) görsel renktir. Büyük levellarda birden fazla ok aynı paleti paylaşabilir. TÜM oyun mantığı (bağlantı, engelleme, buz) `arrowId` üzerinden yürür, `palette` yalnızca boyamadır.

## 4. Çekirdek Algoritmalar

### 4.1 Bağlantı tespiti (traceConnected)
Bir okun tamamlanıp tamamlanmadığı, her döndürme animasyonu bittikten sonra (yalnızca döndürülen taşın okuna) kontrol edilir:

```
cur = tailCell; out = (cur.b + cur.rot) % 6; count = 1
döngü:
  next = hücre(cur + DIRS[out])
  yoksa / arrowId farklıysa / ziyaret edildiyse → BAĞLI DEĞİL
  need = opp(out)
  next head ise:  (next.a + next.rot) % 6 == need değilse BAĞLI DEĞİL
                  count+1 == len ise BAĞLI (exitDir = (next.b+next.rot)%6)
  next mid ise:   d1=(a+rot)%6, d2=(b+rot)%6
                  d1==need → out=d2 ; d2==need → out=d1 ; ikisi de değilse BAĞLI DEĞİL
  cur = next; count++
```
Düz mid segmentlerde (b−a ≡ 3) iki rot değeri de geçerli bağlantı verebilir — bu normaldir, özel durum kodu yazma.

### 4.2 Uçuş ışını ve engelleme (rayBlockers)
Ok bağlandığında head hücresinden `exitDir` yönünde hex-adım adım ilerlenir (üst sınır ~16 adım / tahta dışına çıkana dek):
- Adımdaki pozisyonda hücre varsa VE `arrowId` farklıysa VE o ok `exited` değilse → ENGEL.
- Okun KENDİ hücreleri engel sayılmaz (ışın kendi gövdesinin üzerinden geçebilir).
- Uçmakta olan ok engel sayılMAZ: bir ok uçuşa başladığı AN `exited = true` yapılır ve hücreleri veri katmanından silinir (görsel ayrı yürür). Böylece uçuş sırasında tamamlanan başka bir ok, havadaki oka takılmaz.

### 4.3 Tamamlanma akışı (durum makinesi)
`idle → connected → (flying → done) | (waiting → connected → ...)`
1. Bağlantı kurulunca: ok `connected`, tüm hücreleri KİLİTLENİR (artık döndürülemez; bağlantı asla bozulmaz). ~240ms sonra uçuş denenir.
2. Işın temizse: `flying`. Ok kendi yolu boyunca süzülür (kuyruk yol üzerinde ilerler), head'den sonra `exitDir` doğrultusunda düz devam edip ekrandan çıkar. Uçuş başlarken okun taşları (hexler) kuyruk geçtikçe SIRAYLA küçülüp kaybolur (taş başına gecikme = `√3·S / uçuş_hızı`; prototipte hız 0.55 px/ms, ölçekle orantıla). Uçuş bitince ok `done`; taşlar sahneden tamamen kalkar (hayalet/soluk taş bırakılmaz).
3. Işın engelliyse: ileri atılıp ÇARPIP GERİ DÖNER (bounce, ~560ms, sin eğrisi; ileri mesafe = engel taşının sınırına kadar). Sonra `waiting`: okun TAŞLARI GÖRÜNMEZ olur (segment çizgisi zeminde asılı kalır) — "doğru çözüldü ama önü tıkalı" sinyali. Bekleyen okun taşlarına dokunmak hiçbir şey yapmaz.
4. Her ok çıkışından sonra: `waiting` durumundaki TÜM oklar için ışın yeniden kontrol edilir; temiz olanlar kademeli gecikmeyle (ilki ~180ms, sonrakiler +260ms) otomatik fırlatılır (zincirleme çıkış hissi). Buz eşiği dolanların buzu kırılır (bkz. 5).

## 5. Buz Mekaniği

- Konfigürasyona göre (bkz. 7) 3 ok "buz içinde" başlar: `freezeAt ∈ {1,2,3}` (her eşik tam bir okta).
- **Kural:** Toplam çıkan ok sayısı `exitedCount ≥ freezeAt` olduğu anda o okun buzu kırılır (`unfrozen = true`). Eşikler HERHANGİ bir okun çıkışıyla dolar, belirli bir renkle değil.
- Buzlu okun taşları DÖNDÜRÜLEMEZ. Dokunulursa buz katmanı kısa titrer (shake, ~300ms), hamle SAYILMAZ, timer BAŞLAMAZ.
- Görsel: buzlu taşların üstünde yarı saydam açık mavi hex katmanı + ince çatlak çizgileri. Okun ORTA hücresinde yuvarlak rozet: kalan gereken çıkış sayısı (`freezeAt − exitedCount`, her çıkışta güncellenir). Üst bardaki ok çipinde ❄ + kalan sayı.
- **Kırılma FX:** eşik dolunca her taşın buzu 6 üçgen parçaya (merkez + ardışık iki köşe) ayrılır; parçalar dışa savrulur (taş başına ~24-38 birim), döner (±55-60°), küçülür (0.5x) ve söner (~650ms). Taştan taşa ~50ms kademeli yayılır. Rozet solup kaybolur.
- Buz, ışın engellemesini DEĞİŞTİRMEZ: buzlu okun segmentleri normal engeldir.
- Karıştırma her okun baştan bağlı olmamasını garanti ettiği ve buzlu ok döndürülemediği için, buz kırıldığında ok her zaman "çözülmemiş" durumdadır.

## 6. Renk/Palet Sistemi

10 renklik sabit palet (koyu arka plan `#5b6285` üzerinde, hepsi ayrışır):

```
0 kırmızı #d64545   1 mavi #3d8bfd   2 yeşil #3fae5a   3 turuncu #ef8f3a   4 mor #a259d6
5 turkuaz #26b5ac   6 pembe #e0559b  7 sarı #ecc94b    8 kahve #96693c     9 lacivert #5f5fe0
```

- **Görsel şema:** Taşlar okun PALET RENGİNE boyanır; segment çizgileri, kuyruk noktası ve ok ucu BEYAZDIR. Uçuş animasyonu da beyaz çizer (geçişte renk zıplaması olmaz).
- **Palet ataması = çizge boyama.** Ok komşuluk grafiği çıkarılır (iki ok, herhangi bir hücre çifti komşuysa komşudur). Her oka, komşularında kullanılmayan bir palet atanır; eşit durumda en az kullanılan paletlerden rastgele seçilir (dengeli + çeşitli).
- **Mutlak kural:** Aynı paletteki iki ok HİÇBİR hücrede birbirine değemez. Boyama başarısız olursa o üretim denemesi atılır. (Bu kural ihlal edilirse oyuncu iki ayrı oku tek kopuk ok sanır — geçmişte yaşanmış gerçek bir bug'dır, birim testi şart.)
- Palette tekrar eden hex değeri olamaz — açılışta assert et.

## 7. Level Üretimi (her adım zorunlu)

Üretim deterministik seed'li RNG ile yapılır (prototip: mulberry32). Unity'de `System.Random(seed)` yeterli; aynı seed → aynı level istenirse mulberry32'yi porta birebir taşı.

**Zorluk tablosu (levelConfig):**

| Level | R (bölge yarıçapı) | Ok uzunlukları | minEdges | Buz | Palet |
|-------|-----|----------------|----------|-----|-------|
| 1 | 3 | 3,3,4 | 1 | yok | ayrık |
| 2 | 4 | 3,4,5,6,6 | 2 | yok | ayrık |
| 3-4 | 5 | 3,4,5,6,6,7 | 3 | 3 ok (1/2/3) | ayrık |
| 5+ | 7 | 3,3,4,4,5,5,6,6,7,7,8,8,9,10 (14 ok) | 6 | 3 ok (1/2/3) | 10 renk paylaşımlı |

Bölge: `|q| ≤ R, |r| ≤ R, |q+r| ≤ R`. Kural: 1-2 ok kısa olmalı, diğerleri uzun olabilir.

**Deneme döngüsü (attempt, üst sınır ~400):**
1. **Yol yerleştirme:** Her ok için kendi kendini kesmeyen rastgele yürüyüş (self-avoiding walk). İlk ok merkez civarından başlar; sonrakiler mevcut kümeye KOMŞU boş bir hücreden başlar (kompakt, tek parça görünümlü küme için). Yürüyüş tıkanırsa o ok için yeniden dene (~80), olmuyorsa attempt başarısız.
2. **Segment kurulumu:** Yol hücrelerine tail/mid/head tipleri ve lokal a/b değerleri, çözülmüş hâl `rot=0` olacak şekilde yazılır. Head'in uçuş yönü: %55 olasılıkla girişin karşısı (düz devam), aksi halde girişten farklı rastgele yön.
3. **Palet boyaması** (bkz. 6). Başarısızsa attempt atılır.
4. **Engelleme grafiği:** Çözülmüş konfigürasyonda her ok için ışın taranır; ışın üzerindeki yabancı okların kimlikleri `blockedBy[arrowId]` kümesine yazılır.
5. **DAG şartı:** `blockedBy` grafiğinde Kahn topolojik sıralaması TÜM okları sıralayamıyorsa (döngü = A↔B birbirini kilitler = ÇÖZÜLEMEZ) attempt atılır. Bu kontrol pazarlıksızdır.
6. **Karmaşıklık hedefi:** Toplam engelleme kenarı `minEdges`'i geçen ilk iyi attempt seçilir; hiçbiri geçmezse en yüksek kenarlı geçerli attempt kullanılır.
7. **Buz ataması:** Rastgele 3 ok + 1/2/3 eşikleri; her atama TAM SİMÜLASYONLA doğrulanır: "çıkabilecek ok" = eşiği dolu VE tüm blockedBy'ları çıkmış — bu kuralla tüm oklar sırayla çıkabiliyor mu? Çıkamıyorsa yeni atama dene (~80). Hiçbiri tutmazsa GARANTİLİ fallback: topolojik sıranın SON 3 oku 1/2/3 eşikleriyle donar (ispatlanabilir şekilde her zaman çözülebilir). Not: geçerli bir atama varsa oyuncu hangi sırayla çıkarırsa çıkarsın kilitlenemez (çıkışlar monoton fayda sağlar) — yine de simülasyon şarttır.
8. **Karıştırma (scramble):** Her hücreye rastgele `rot` (0-5). Sonra HER ok için bağlantı kontrolü: baştan bağlı gelen ok varsa head hücresinin rot'u bozulur (head'in geçerli rot'u tekildir, garanti kopartır). Hiçbir ok level başında bağlı olamaz.
9. **Öz-denetim:** Her okun ardışık hücreleri axial komşu mu? Değilse konsola hata + yeniden üretim. Palet tekrarı assert'i açılışta.

## 8. Sayaçlar ve İstatistik

- **Hamle sayacı:** Yalnızca GERÇEKLEŞEN döndürmeler sayılır. Buzlu taşa, kilitli (connected/waiting) okun taşına veya boş alana dokunmak saymaz.
- **Süre:** Oyuncunun İLK gerçek döndürmesiyle başlar (level yüklenince DEĞİL). Kazanınca durur. Format `m:ss`. Buzlu taşa dokunmak süreyi başlatmaz.
- Üst barda canlı gösterilir (⏱ 0:00 · N hamle). Kazanma ekranında toplam hamle + süre yazılır. Yeni levelda ve ↻ (aynı zorlukta yeni bulmaca) butonunda ikisi de sıfırlanır.
- Üst barda ayrıca ok başına bir renk çipi: çıkan okta ✓, buzluda ❄ + kalan eşik.

## 9. Görsel Stil Sabitleri (S = hex köşe yarıçapına oranla)

- Arka plan: `#5b6285` → `#4e5476` hafif radyal geçiş. Taşlarda GÖLGE YOK (drop shadow kullanılmaz).
- Taş: palet renginde, köşeleri hafif yuvarlatılmış, taşlar arası ince doğal boşluk (prototipte S−3 iç poligon + kalın aynı-renk stroke etkisi).
- Segment çizgisi: beyaz, kalınlık ≈ **0.153·S**, uçlar yuvarlak. Kuyruk noktası yarıçapı ≈ **0.176·S**. Ok ucu üçgeni: uzunluk ≈ 0.41·S, yarım genişlik ≈ 0.25·S; ucu, head hücre merkezinden 0.62·apothem uzaklıktaki noktadan ileri bakar.
- Döndürme animasyonu: 60°, ~160ms, hafif overshoot'lu ease. Dönme merkezi hücre merkezi.
- Uçuş: ~0.55 px/ms (S=34 ölçeğinde; S başına saniyede ~16 hücre ölçüsünü koru). Bounce ~560ms. Taş yok olma: 450ms scale(0.25)+30° dönme+fade.
- Buz: dolgu `rgba(198,230,255,0.62)`, kenar `rgba(240,250,255,0.95)`, çatlaklar `rgba(255,255,255,0.75)`. Rozet: açık mavi daire, koyu lacivert kalın rakam.
- Bekleyen (waiting) okun taşları opacity 0'a iner (~450ms); segment beyaz ve tam görünür kalır.

## 10. Port Sırasında YAZILMASI ZORUNLU Birim/Entegrasyon Testleri

Bunların her biri prototip geliştirilirken gerçek bug'ları yakaladı; Unity portunda da otomatik test olarak bulunMALIdır (öneri: her test ≥100 rastgele seed):

1. **Çözülebilirlik:** Üretilen her levelda tüm hücreler `rot=0` yapılınca HER ok bağlı olmalı.
2. **Karışıklık:** Level başlangıç durumunda HİÇBİR ok bağlı olmamalı.
3. **Deadlock yok:** Dinamik simülasyon (bağlan→ışın kontrol→çıkar→tekrarla) buz kuralları dahil tüm okları çıkarabilmeli; hiçbir seed'de kilitlenme olmamalı.
4. **Bitişiklik:** Her okun `cells` dizisinde ardışık hücreler axial komşu; hücre sayısı = uzunluk toplamı.
5. **Yapı:** Her okta tam 1 tail (dizide ilk), tam 1 head (son), arası mid; tüm hücreler aynı `arrowId`.
6. **Palet ayrımı:** Aynı paletteki iki okun hücreleri hiçbir yerde komşu değil; buz ataması tam 3 ok ve eşikler {1,2,3}.

## 11. Unity'ye Çeviri Notları

- Önce SALT MANTIK katmanını (grid, üretim, traceConnected, rayBlockers, simülasyon) MonoBehaviour'suz, saf C# sınıfları olarak yaz; testleri EditMode'da koş. Görsel katman sonra gelir.
- Hücre erişimi: `Dictionary<(int q,int r), Cell>` — prototipteki `"q,r"` string key yerine tuple/struct key kullan.
- Taş seçimi için fizik raycast'e gerek yok: dokunma noktasını axial koordinata ters dönüşümle çevir (`q = x/(1.5S)`, `r = y/(√3·S) − q/2`, sonra en yakın hücreye yuvarla — hex rounding: küp koordinatta yuvarlayıp en büyük sapan ekseni düzelt).
- Segmentler için LineRenderer veya tek mesh; uçuş animasyonu için okun tam yol polyline'ı (kuyruk merkezi → kenar ortaları → her hücrenin MERKEZİ → head ucu → uzatma) üzerinde ilerleyen bir "pencere" yaklaşımı prototiple birebir aynı hissi verir.
- Bir hücrenin dünya polyline'ı hesaplanırken de merkezden geçme kuralı geçerli (kenar ortası → merkez → kenar ortası).
- Mobil: dokunma hedefi taşın tamamı; 60° dönüş inputu debounce etme, arka arkaya hızlı dokunuşlar birikerek dönmeli (prototipte her tap +60° hedef açıya eklenir).
- Zamanlayıcı `Time.unscaledDeltaTime` ile; uygulama arka plana giderse duraklatma kararını ürün tarafına sor.
- Seed'i level numarasına bağla ki aynı level tekrar oynanabilsin/debug edilebilsin; hata raporlarında seed'i logla.

## 12. Bilinen Tuzaklar (geçmişte yaşandı, tekrarlama)

- **Renk = kimlik sanmak:** İki ayrı ok aynı palete boyanınca oyuncu "ok kopuk" sanır. Mantıkta daima `arrowId`, görselde `palette`. Test 6 bunun bekçisidir.
- **Segmenti köşe keserek çizmek:** Mid segment iki kenar ortasını DÜZ çizgiyle bağlarsa yanlış — daima merkez üzerinden kırık çizgi.
- **Konfig/kod sabiti kopukluğu:** Ok sayısını sabit yazmak yerine daima `lengths.Count`'tan türet; palet sayısını configden al ve palet dizisi uzunluğuyla assert'le.
- **Uçan oku engel saymak:** Uçuş başında exited işaretlenmezse, uçuş sırasında tamamlanan ok havadaki oka "çarpar".
- **Y ekseni aynalaması:** SVG→Unity geçişinde açı/dönme yönü ters dönebilir; bölüm 2'deki uyarıyı ilk gün testle sabitle.
