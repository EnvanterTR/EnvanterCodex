#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)"
DOMAIN="${1:-}"
CONFIG_FILE="/etc/envantertr/server.env"

fail() { printf 'HATA: %s\n' "$*" >&2; exit 1; }
confirm() {
  local prompt="$1" answer
  read -r -p "$prompt [e/H] " answer || return 1
  [[ "${answer,,}" == e || "${answer,,}" == evet || "${answer,,}" == y || "${answer,,}" == yes ]]
}
package_is_installed() {
  local package="$1" status
  status="$(dpkg-query -W -f='${db:Status-Status}' "$package" 2>/dev/null || true)"
  [[ "$status" == installed ]]
}

[[ "$EUID" -eq 0 ]] || fail 'Yardımcıyı sudo ile root olarak başlatın: sudo bash deploy/server/install-guided.sh alanadiniz.com'
[[ -r /etc/os-release ]] || fail 'İşletim sistemi bilgisi okunamadı.'
# shellcheck disable=SC1091
source /etc/os-release
[[ "${ID:-}" == ubuntu && "${VERSION_ID:-}" == 24.04 ]] || fail 'Bu kurulum yardımcısı yalnızca Ubuntu Server 24.04 LTS için doğrulanmıştır.'
[[ "$(dpkg --print-architecture)" == amd64 ]] || fail 'Bu kurulum paketi şu an x86-64 (amd64) sunucular içindir.'
command -v systemctl >/dev/null || fail 'systemd bulunamadı; desteklenen sunucu profilinde systemd gerekir.'
command -v apt-get >/dev/null || fail 'Ubuntu apt paket yöneticisi bulunamadı.'
command -v dpkg-query >/dev/null || fail 'dpkg-query bulunamadı.'

if [[ -e "$CONFIG_FILE" || -e /opt/envantertr/current || -L /opt/envantertr/current ]]; then
  fail 'Bu sunucuda EnvanterTR kurulumu zaten var. Yeni kurulum başlatmadım; güncelleme veya kurtarma akışını kullanın.'
fi

if [[ -z "$DOMAIN" ]]; then
  read -r -p 'Bu sunucuya yönlenen DNS alan adı (ör. envanter.sirketiniz.com): ' DOMAIN || fail 'Alan adı alınamadı.'
fi
[[ "$DOMAIN" =~ ^[A-Za-z0-9]([A-Za-z0-9.-]{0,251}[A-Za-z0-9])?$ && "$DOMAIN" != *..* && "${DOMAIN,,}" != localhost ]] || \
  fail 'Geçerli bir genel DNS alan adı girin; localhost kullanılamaz.'
command -v getent >/dev/null || fail 'DNS ön kontrolü için getent bulunamadı.'
getent ahosts "$DOMAIN" >/dev/null || fail "'$DOMAIN' DNS üzerinden çözümlenemedi. Genel DNS A/AAAA kayıtlarını bu sunucuya yönlendirin, bekleyin ve yeniden deneyin."

printf '\nKurulumdan önce kontrol edin:\n'
printf '  • %s DNS kaydı bu sunucuya yönleniyor.\n' "$DOMAIN"
printf '  • Sunucuya dış ağdan TCP 80 ve 443 erişimi var; Caddy HTTPS sertifikası alacak.\n'
printf '  • Kurulum Docker servislerini başlatacak ve yalnızca EnvanterTR için adlandırılmış volume oluşturacak.\n'
printf '  • Güvenlik duvarı kuralını otomatik değiştirmeyeceğim. Docker yayımlı portları UFW kurallarını atlayabildiği için dış erişimi ağ/güvenlik duvarı politikanızdan sınırlayın.\n'
confirm 'DNS ve ağ koşullarını doğruladınız mı? Devam edilsin mi?' || fail 'Kurulum kullanıcı isteğiyle başlamadan durduruldu; sistem değiştirilmedi.'

docker_ready=false
if command -v docker >/dev/null && docker compose version >/dev/null 2>&1; then
  if docker info >/dev/null 2>&1; then
    docker_ready=true
  elif confirm 'Docker Engine ve Compose kurulu fakat çalışmıyor. Docker servisini şimdi başlatayım mı?'; then
    systemctl enable --now docker
    docker info >/dev/null 2>&1 || fail 'Docker servisi başlatılamadı; hiçbir EnvanterTR kurulumu başlatılmadı.'
    docker_ready=true
  else
    fail 'Docker servisi başlatılmadı; hiçbir EnvanterTR kurulumu yapılmadı.'
  fi
fi

if [[ "$docker_ready" != true ]]; then
  for package in docker.io docker-compose docker-compose-v2 docker-doc docker-buildx podman-docker containerd runc; do
    if package_is_installed "$package"; then
      fail "Mevcut paket '$package' Docker kurulumu ile çakışabilir. Yardımcı mevcut paketleri kaldırmaz veya değiştirmez; sunucunuzun yöneticisiyle Docker Engine/Compose v2 kurulumu netleştirin."
    fi
  done
  if command -v docker >/dev/null; then
    fail 'Docker komutu var ancak çalışan Docker Engine ve Compose v2 bulunamadı. Mevcut Docker kurulumunu değiştirmedim.'
  fi
  if ! confirm 'Docker Engine ve Compose v2 eksik. Docker’ın resmi Ubuntu deposunu ekleyip bu bileşenleri kurayım mı?'; then
    fail 'Docker kurulumu onaylanmadı; sistem değiştirilmedi.'
  fi
  command -v curl >/dev/null || fail 'Docker deposu için curl eksik; curl kurup yeniden deneyin.'
  command -v install >/dev/null || fail 'install komutu bulunamadı.'
  command -v cmp >/dev/null || fail 'cmp komutu bulunamadı.'

  keyring_dir=/etc/apt/keyrings
  keyring_file="$keyring_dir/docker.asc"
  source_file=/etc/apt/sources.list.d/docker.sources
  expected_source="Types: deb
URIs: https://download.docker.com/linux/ubuntu
Suites: ${UBUNTU_CODENAME:-$VERSION_CODENAME}
Components: stable
Architectures: $(dpkg --print-architecture)
Signed-By: $keyring_file"
  temp_dir="$(mktemp -d /tmp/envantertr-docker-repo.XXXXXXXX)"
  trap 'rm -rf -- "$temp_dir"' EXIT
  printf '%s\n' "$expected_source" > "$temp_dir/docker.sources"
  if [[ -e "$source_file" ]]; then
    cmp -s "$temp_dir/docker.sources" "$source_file" || fail "$source_file başka bir kaynağa ait; üzerine yazmadım. Kaynak dosyasını yöneticiyle inceleyin."
  fi
  if [[ -e "$keyring_file" ]]; then
    grep -q '^-----BEGIN PGP PUBLIC KEY BLOCK-----$' "$keyring_file" || fail "$keyring_file mevcut ancak beklenen biçimde değil; üzerine yazmadım."
  fi

  printf '\nDocker’ın resmî apt deposu kullanılacak. Mevcut paketleri kaldırmayacağım.\n'
  export DEBIAN_FRONTEND=noninteractive
  apt-get update
  apt-get install -y ca-certificates curl
  install -d -m 0755 "$keyring_dir"
  if [[ ! -e "$keyring_file" ]]; then
    curl --fail --silent --show-error --location https://download.docker.com/linux/ubuntu/gpg -o "$temp_dir/docker.asc"
    grep -q '^-----BEGIN PGP PUBLIC KEY BLOCK-----$' "$temp_dir/docker.asc" || fail 'Docker imza anahtarı beklenen biçimde değil; apt kaynağı eklenmedi.'
    install -m 0644 "$temp_dir/docker.asc" "$keyring_file"
  fi
  if [[ ! -e "$source_file" ]]; then install -m 0644 "$temp_dir/docker.sources" "$source_file"; fi
  apt-get update
  apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
  systemctl enable --now docker
  docker info >/dev/null 2>&1 || fail 'Docker kurulumu tamamlandı ancak daemon hazır değil; EnvanterTR kurulumu başlatılmadı.'
  docker compose version >/dev/null 2>&1 || fail 'Docker Compose v2 doğrulanamadı; EnvanterTR kurulumu başlatılmadı.'
fi

[[ -f "$SCRIPT_DIR/install.sh" && -f "$SCRIPT_DIR/compose.yaml" ]] || fail 'Sunucu kurulum dosyaları kaynak paket içinde eksik.'
if [[ -n "${temp_dir:-}" ]]; then
  rm -rf -- "$temp_dir"
  trap - EXIT
fi
printf '\nÖn kontroller tamamlandı; EnvanterTR uygulama kurulumu başlıyor.\n\n'
exec bash "$SCRIPT_DIR/install.sh" "$DOMAIN"
