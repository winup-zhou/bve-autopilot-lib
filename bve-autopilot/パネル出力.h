// パネル出力.h : 運転台パネルへ出力する値を定義します
//
// Copyright © 2019 Watanabe, Yuki
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA  02110 - 1301  USA

#pragma once
#include <functional>
#include <string>
#include <utility>

namespace autopilot
{

    class Main;

    class パネル出力対象 {
    public:
        パネル出力対象(const std::function<int(const Main &)> & 出力) :
            _出力(出力) { }
        パネル出力対象(std::function<int(const Main &)> && 出力) :
            _出力(std::move(出力)) { }

        int 出力(const Main & main) const { return _出力(main); }

        static パネル出力対象 対象(const std::wstring & 名前);

    private:
        std::function<int(const Main &)> _出力;
    };

    // ホスト（MetroAtsBridge）向け：ini の端子設定とは無関係に、
    // 全論理出力（パネル出力対象名簿）を番号で直接参照する内部通信用アクセサ。
    int パネル出力対象数() noexcept;
    const wchar_t *パネル出力対象名(int 索引) noexcept;
    int パネル出力対象値(const Main &main, int 索引);

}
